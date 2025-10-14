# Backlog - Агрегатор биржевых спредов

## Рефакторинг 1.1: Упрощение и исправление архитектуры
- [x] Провести аудит кода и выявить проблемы.
- [ ] **Упростить DI:** Заменить `AggregatedExchangeClient` на `BinanceExchangeClient`.
- [ ] **Исправить DI:** Внедрить `VolumeFilter` через конструктор в `OrchestrationService`.
- [ ] **Удалить лишний код:** Убрать `SimpleHttpServer` из `Program.cs`.
- [ ] **Удалить лишние файлы:** Удалить `AggregatedExchangeClient`, `ExchangeFactory`, `SimpleHttpServer`.
- [x] **Обновить `backlog.md`:** Актуализировать задачи.

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
- [x] Установить `Binance.Net` (вместо `JKorf.Exchange.Net`).
- [x] Реализовать `BinanceExchangeClient` для получения данных с биржи.
- [x] Реализовать `FleckWebSocketServer` для трансляции данных.

### Слой приложения (Application)
- [x] Создать `OrchestrationService` для управления потоками данных.

### Слой представления (Presentation)
- [x] Настроить DI-контейнер.
- [x] Реализовать `Program.cs` для запуска приложения.