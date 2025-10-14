# Backlog - Агрегатор биржевых спредов

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