Минимальный срез архитектуры для проверки эффективности

## Что это

- `Server/` - ASP.NET Core 8 приложение с одним SignalR hub (`/battleHub`), broadcast всем подключённым.
- `Server/wwwroot/observer.html` - панель наблюдателя
- `Server/wwwroot/dashboard.html` - живая лента для главного коллера


## Запуск локально

```bash
cd Server
dotnet run
```

По умолчанию приложение поднимется на `https://localhost:5001` (или похожий порт, смотрите
вывод в консоли). Откройте в браузере:

- `https://localhost:5001/observer.html` - для наблюдателя
- `https://localhost:5001/dashboard.html` - для командования

Локальный сервер недоступен снаружи вашей сети напрямую. Проще всего пробросить порт через
туннель на время теста, например:

```bash
# любой из вариантов, выберите то, что уже установлено
ngrok http https://localhost:5001
# или
cloudflared tunnel --url https://localhost:5001
```




