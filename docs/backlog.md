# Backlog - Агрегатор биржевых спредов

## Спринт 4: Интеграция OKX
- [x] **Задача 1: Подготовка и настройка**
    - [x] Добавить NuGet-пакет `JK.OKX.Net` в проект `SpreadAggregator.Infrastructure`.
    - [x] Создать файл `OkxExchangeClient.cs` в директории `src/SpreadAggregator.Infrastructure/Services/Exchanges/`.
    - [x] Зарегистрировать `OkxExchangeClient` в DI-контейнере в `Program.cs`.
    - [x] Добавить секцию "OKX" в `appsettings.json` с параметрами фильтрации по объему.
- [x] **Задача 2: Реализация `OkxExchangeClient`**
    - [x] Реализовать интерфейс `IExchangeClient`.
    - [x] Реализовать метод `GetTickersAsync` для получения всех тикеров и их 24-часового объема.
    - [x] Реализовать метод `SubscribeToTickersAsync` для подписки на обновления книги ордеров (book ticker).
- [ ] **Задача 3: Тестирование и отладка**
    - [ ] Проверить корректность получения данных по объему и фильтрации символов.
    - [ ] Убедиться в стабильности WebSocket-подписки и корректности получаемых данных о спреде.

## Рефакторинг
- [x] **Оптимизация формата данных WebSocket:**
    - [x] Создан `SpreadDataPackage` DTO для компактного представления данных.
    - [x] `OrchestrationService` обновлен для формирования нового формата (массив массивов).
    - [x] `client.html` обновлен для парсинга нового формата.
- [x] **Устранение OutOfMemoryException:**
    - [x] Создан `SpreadDataCache` для агрегации данных о спредах.
    - [x] `OrchestrationService` переработан для использования кэша.
    - [x] Реализована фоновая отправка данных из кэша раз в секунду для снижения нагрузки на WebSocket.
- [x] **Удаление OKX:**
    - [x] Удалена зависимость `OKX.Api` из проекта `SpreadAggregator.Infrastructure`.
    - [x] Проведена очистка артефактов сборки для полного удаления связанных файлов.

## Спринт 3: Интеграция KuCoin
- [x] **Интеграция с KuCoin:**
    - [x] Добавлен пакет `Kucoin.Net`.
    - [x] Создан `KucoinExchangeClient`.
    - [x] Исправлены ошибки компиляции, связанные со структурой данных `KucoinStreamBestOffers`.
    - [x] `KucoinExchangeClient` зарегистрирован в DI.
    - [x] `appsettings.json` обновлен для поддержки KuCoin.

## Спринт 2: Интеграция MEXC и рефакторинг
- [x] **Рефакторинг DI и OrchestrationService:**
    - [x] Упрощен `IExchangeClient` (удален метод `GetClient`).
    - [x] Удален неиспользуемый `ExchangeFactory`.
    - [x] `OrchestrationService` переработан для работы с `IEnumerable<IExchangeClient>`.
    - [x] Логика запуска обработки бирж вынесена в `OrchestrationService` и основывается на конфигурации.
- [x] **Интеграция с MEXC:**
    - [x] Добавлен пакет `JK.Mexc.Net`.
    - [x] Создан `MexcExchangeClient`.
    - [x] `MexcExchangeClient` зарегистрирован в DI.
    - [x] `appsettings.json` обновлен для поддержки MEXC.
- [x] **Тестирование и отладка:**
    - [x] Исправлена ошибка с размером пакета подписки для MEXC.
    - [x] Скорректированы параметры фильтрации для MEXC.

## MVP 1.0: Основной функционал (Завершено)

### Архитектура и настройка
- [x] Создать структуру проекта (Domain, Application, Infrastructure, Presentation, Tests).
- [x] Настроить зависимости между проектами.
- [x] Создать `appsettings.json` с базовой конфигурацией.
- [x] Создать `docs/backlog.md` для ведения задач.

### Слой домена (Domain)
- [x] Создать сущность `SpreadData` для хранения информации о спреде.
- [x] Реализовать сервис `SpreadCalculator` с методом для расчета спреда.
- [x] Реализовать сервис `VolumeFilter` с методом для фильтрации пар по объему.

### Тестирование (TDD)
- [ ] Написать Unit-тест для `SpreadCalculator`.
- [ ] Написать Unit-тест для `VolumeFilter`.

### Слой инфраструктуры (Infrastructure)
- [x] Установить `Binance.Net`.
- [x] Реализовать `BinanceExchangeClient` для получения данных с биржи.
- [x] Реализовать `FleckWebSocketServer` для трансляции данных.

### Слой приложения (Application)
- [x] Создать `OrchestrationService` для управления потоками данных.

### Слой представления (Presentation)
- [x] Настроить DI-контейнер.
- [x] Реализовать `Program.cs` для запуска приложения.