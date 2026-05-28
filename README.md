<div align="center">

<img src="docs/app.png" width="150" alt="YandexRPC">

# YandexRPC

**Discord Rich Presence для Яндекс Музыки**

Показывает в вашем профиле Discord трек, который играет прямо сейчас — с обложкой, полосой прогресса и кнопками.

<br>

[![Релиз](https://img.shields.io/github/v/release/Eneryleen/YandexRPC?style=for-the-badge&label=%D0%A1%D0%BA%D0%B0%D1%87%D0%B0%D1%82%D1%8C&color=FFCC00&labelColor=1a1a1a)](https://github.com/Eneryleen/YandexRPC/releases/latest)
[![Платформа](https://img.shields.io/badge/Windows%2010%2F11-0078D6?style=for-the-badge&logo=windows&logoColor=white)](#требования)
[![Лицензия](https://img.shields.io/github/license/Eneryleen/YandexRPC?style=for-the-badge&label=%D0%9B%D0%B8%D1%86%D0%B5%D0%BD%D0%B7%D0%B8%D1%8F&labelColor=1a1a1a&color=4c7)](LICENSE)

[![Сборка](https://github.com/Eneryleen/YandexRPC/actions/workflows/release.yml/badge.svg)](https://github.com/Eneryleen/YandexRPC/actions/workflows/release.yml)

<br>

<img src="docs/yandex-music.png" width="280" alt="Яндекс Музыка">

</div>

---

## Как это работает

1. Читает «что сейчас играет» из системного медиа-центра Windows (SMTC) — того самого, что появляется при нажатии кнопок громкости. **Логин в Яндекс не нужен.**
2. По названию и исполнителю находит обложку и ссылку через публичный поиск Яндекс Музыки.
3. Отправляет статус в Discord как **«Слушает»**.

## Возможности

| Возможность | Описание |
|---|---|
| **Статус «Слушает»** | Название трека и исполнитель прямо в профиле Discord |
| **Обложка и прогресс** | Обложка альбома и полоса «сколько осталось до конца» |
| **Кнопки** | «Открыть» (трек), «Скачать RPC» и свои — до 2 одновременно |
| **Трей и автозапуск** | Иконка в трее с настройками, запуск вместе с Windows |
| **Работает «из коробки»** | Discord-приложение уже встроено, ничего создавать не надо |

## Требования

- Windows 10 версии 1809 или новее (нужен системный медиа-центр).
- Установленное приложение **Яндекс Музыка** для ПК — именно оно отдаёт данные в систему.
- Запущенный десктоп-клиент **Discord**.

## Установка

1. Скачайте **[последнюю версию](https://github.com/Eneryleen/YandexRPC/releases/latest)** — файл `YandexRPC-win-Setup.exe`.
2. Запустите его. Приложение установится для текущего пользователя и появится в трее.
3. Обновления потом ставятся автоматически.

> Есть и портативная версия — `YandexRPC-win-Portable.zip`, без установки.

## Настройка

Откройте трей → **Настройки…**. Всё работает сразу, но при желании можно поменять:

- **Кнопки** — «Открыть» (ссылка на трек) и «Скачать RPC» (на этот репозиторий) включаются галочками.
- **Свои кнопки** — по одной на строку в формате `Название | https://ссылка`.
- **Скрывать на паузе**, **автозапуск**, **включить/выключить** статус.

> Discord показывает **максимум 2 кнопки**. Приоритет: «Открыть» → ваши кнопки → «Скачать RPC».

<details>
<summary><b>Свой Discord Application ID (необязательно)</b></summary>

<br>

По умолчанию встроено готовое приложение **Yandex Music**, и менять ничего не нужно. Но если хотите своё:

1. Откройте [Discord Developer Portal](https://discord.com/developers/applications) → **New Application**.
2. Скопируйте **Application ID** → вставьте в настройках.
3. Для маленьких значков «играет/пауза» и запасной картинки загрузите в *Rich Presence → Art Assets* ассеты с именами `logo`, `play`, `pause`. Обложка трека подтягивается по ссылке и работает без ассетов.

</details>

## Сборка из исходников

Нужен [.NET 8 SDK](https://dotnet.microsoft.com/download) и **Windows**:

```powershell
dotnet publish src/YandexRPC/YandexRPC.csproj -c Release -r win-x64 --self-contained
```

Релизы собираются автоматически: пуш тега `v*` запускает [GitHub Actions](.github/workflows/release.yml), который упаковывает установщик через [Velopack](https://velopack.io) и публикует в Releases.

## Ограничения

- Работу трея, чтения трека и связи с Discord нужно проверять на реальной Windows — проект разрабатывался не на ней.
- Идентификатор SMTC-сессии Яндекса может отличаться в разных версиях приложения — если статус не появляется, это первое, что стоит проверить.
- Опция «плавные переходы между треками» (crossfade) в Яндекс Музыке может ломать передачу данных в систему.
- Discord обновляет статус при смене трека и паузе, а не каждую секунду (ограничение Discord).

## Лицензия

[MIT](LICENSE) © Eneryleen

<div align="center">
<sub>Логотип «Яндекс Музыка» принадлежит Яндексу и используется только для обозначения совместимости.</sub>
</div>
