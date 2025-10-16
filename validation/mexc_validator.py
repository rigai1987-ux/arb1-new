import asyncio
import json
import ccxt.pro as ccxtpro
import websockets
from decimal import Decimal, getcontext
import warnings

# Suppress protobuf warnings
warnings.filterwarnings('ignore', category=UserWarning, module='google.protobuf.runtime_version')

# --- Configuration ---
getcontext().prec = 18 # Set precision for Decimal
C_SHARP_WSS_URL = "ws://127.0.0.1:8181"
TARGET_EXCHANGE = "MEXC"

# --- Queues for data sharing ---
csharp_queue = asyncio.Queue()
ccxt_queue = asyncio.Queue()
# Queue to signal which symbol to watch
symbol_to_watch_queue = asyncio.Queue(maxsize=1)

# --- Data Consumers ---

async def csharp_client(url, exchange_name):
    """
    Connects to the C# WebSocket server, identifies the first active symbol for the target exchange,
    and puts its data into the queue.
    """
    while True:
        try:
            print(f"Attempting to connect to C# WebSocket server at {url}...", flush=True)
            async with websockets.connect(url) as websocket:
                print(f"C# client connected. Waiting for data from '{exchange_name}'...", flush=True)
                while True:
                    message = await websocket.recv()
                    package = json.loads(message)
                    fields = package.get("Fields", [])
                    data = package.get("Data", [])

                    if not fields or not data:
                        continue

                    for row_data in data:
                        row_object = {fields[i]: row_data[i] for i in range(len(fields))}
                        
                        if row_object.get("exchange") == exchange_name:
                            symbol = row_object.get("symbol")
                            # Signal the symbol to the ccxt_client if it's the first time
                            if symbol_to_watch_queue.empty():
                                print(f"C# client detected active symbol for {exchange_name}: {symbol}. Signaling CCXT client.", flush=True)
                                await symbol_to_watch_queue.put(symbol)

                            # Only queue data for the symbol we are actively watching
                            if not symbol_to_watch_queue.empty() and symbol == symbol_to_watch_queue._queue[0]:
                                await csharp_queue.put({
                                    "symbol": symbol,
                                    "bid": Decimal(str(row_object["bestBid"])),
                                    "ask": Decimal(str(row_object["bestAsk"]))
                                })
        except (websockets.exceptions.ConnectionClosed, ConnectionRefusedError) as e:
            print(f"C# client connection failed: {e}. Is the C# application running? Retrying in 5s...", flush=True)
            await asyncio.sleep(5)
        except Exception as e:
            print(f"An unexpected error occurred in C# client: {e}", flush=True)
            await asyncio.sleep(5)

async def ccxt_client(exchange_name):
    """
    Waits for a symbol signal, then connects to CCXT Pro and puts order book data into the queue.
    """
    print(f"CCXT client for {exchange_name} waiting for symbol signal...", flush=True)
    symbol = await symbol_to_watch_queue.get() # This will block until a symbol is available
    print(f"CCXT client received signal. Connecting to {exchange_name} for symbol {symbol}...", flush=True)
    
    exchange = getattr(ccxtpro, exchange_name.lower())()
    print(f"CCXT Pro {exchange_name} exchange object created.", flush=True)
    while True:
        try:
            orderbook = await exchange.watch_order_book(symbol)
            await ccxt_queue.put({
                "symbol": symbol,
                "bid": Decimal(str(orderbook['bids'][0][0])),
                "ask": Decimal(str(orderbook['asks'][0][0]))
            })
        except (ccxtpro.base.errors.NetworkError, asyncio.CancelledError) as e:
            print(f"CCXT client connection closed or network error: {e}", flush=True)
            await exchange.close()
            break
        except Exception as e:
            print(f"Error in CCXT client for symbol {symbol}: {e}", flush=True)
            await exchange.close()
            await asyncio.sleep(5)
            exchange = getattr(ccxtpro, exchange_name.lower())() # Re-initialize

# --- Validator ---

def calculate_diff_percent(val1, val2):
    if val1 == 0:
        return float('inf') if val2 != 0 else 0
    return abs((val1 - val2) / val1) * 100

async def validator():
    """Compares data from both queues for the same symbol."""
    print("Validator task started. Waiting for data...", flush=True)
    csharp_data = {}
    ccxt_data = {}

    while True:
        # Drain queues to get the latest data
        while not csharp_queue.empty():
            item = csharp_queue.get_nowait()
            csharp_data[item['symbol']] = item
        
        while not ccxt_queue.empty():
            item = ccxt_queue.get_nowait()
            ccxt_data[item['symbol']] = item

        # Compare data for common symbols
        common_symbols = csharp_data.keys() & ccxt_data.keys()
        for symbol in common_symbols:
            ccxt_item = ccxt_data[symbol]
            csharp_item = csharp_data[symbol]

            ccxt_bid = ccxt_item['bid']
            ccxt_ask = ccxt_item['ask']
            csharp_bid = csharp_item['bid']
            csharp_ask = csharp_item['ask']

            if ccxt_bid == csharp_bid and ccxt_ask == csharp_ask:
                print(f"OK [{symbol}]: CCXT(Bid:{ccxt_bid:.8f}, Ask:{ccxt_ask:.8f}) == C#(Bid:{csharp_bid:.8f}, Ask:{csharp_ask:.8f})", flush=True)
            else:
                bid_diff = calculate_diff_percent(ccxt_bid, csharp_bid)
                ask_diff = calculate_diff_percent(ccxt_ask, csharp_ask)
                print(f"--- MISMATCH DETECTED [{symbol}] ---", flush=True)
                print(f"  Source | {'Bid'.ljust(22)} | {'Ask'.ljust(22)}", flush=True)
                print(f"  -------------------------------------------------------------", flush=True)
                print(f"  CCXT   | {f'{ccxt_bid:.8f}'.ljust(22)} | {f'{ccxt_ask:.8f}'.ljust(22)}", flush=True)
                print(f"  C#     | {f'{csharp_bid:.8f}'.ljust(22)} | {f'{csharp_ask:.8f}'.ljust(22)}", flush=True)
                print(f"  Diff % | {f'{bid_diff:.6f}%'.ljust(22)} | {f'{ask_diff:.6f}%'.ljust(22)}", flush=True)
                print("------------------------------------", flush=True)
            
            # Remove compared data
            del csharp_data[symbol]
            del ccxt_data[symbol]

        await asyncio.sleep(0.1) # Check for new data every 100ms

# --- Main Execution ---

async def main():
    print("Validator main() started.", flush=True)
    csharp_task = asyncio.create_task(csharp_client(C_SHARP_WSS_URL, TARGET_EXCHANGE))
    ccxt_task = asyncio.create_task(ccxt_client(TARGET_EXCHANGE))
    validator_task = asyncio.create_task(validator())
    print("All tasks created.", flush=True)

    done, pending = await asyncio.wait(
        [csharp_task, ccxt_task, validator_task],
        return_when=asyncio.FIRST_COMPLETED,
    )

    print(f"A task has completed. Done: {done}", flush=True)
    for task in pending:
        task.cancel()

if __name__ == "__main__":
    print("Script entry point.", flush=True)
    try:
        print(f"Starting Dynamic Symbol Validator for {TARGET_EXCHANGE}...", flush=True)
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nValidator stopped by user.", flush=True)
    except Exception as e:
        print(f"An unhandled exception occurred in main: {e}", flush=True)
    finally:
        print("Shutdown complete.", flush=True)