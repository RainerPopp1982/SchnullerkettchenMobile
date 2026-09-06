# Build-Workflow: Android & iOS

Diese Anleitung fasst den kompletten, praxiserprobten Ablauf zusammen, um aus
`SchnullerkettchenMobile` eine installierbare **Android-APK** (lokal) oder
**iOS-IPA** (über GitHub Actions) zu bauen – inklusive aller Stolperfallen,
die dabei tatsächlich aufgetreten sind.

Für die *einmalige* Einrichtung von Apple-Zertifikat, Provisioning-Profil und
GitHub-Secrets siehe **`IOS-CI-ANLEITUNG.md`**. Diese Datei hier ist der
Alltags-Leitfaden: "Wie baue ich gerade jetzt eine neue Version?"

---

## Wichtige Grundregel: Visual Studio NICHT zum Bauen verwenden

Visual Studio 2022 kann `net10.0-android` / `net10.0-ios` **nicht** als Ziel
bauen (Fehler `NETSDK1209`: *"Die aktuelle Visual Studio-Version unterstützt
.NET 10.0 nicht als Ziel ... verwenden Sie eine Version von Visual Studio
18.0 oder höher"*). Erst **Visual Studio 2026 (v18)** unterstützt .NET-10-
MAUI-Mobiltargets vollständig – bis dahin gilt:

- **Nicht** über den grünen Start-Button, "Build Solution" oder "Publish" in
  VS bauen.
- VS nur zum **Bearbeiten** des Codes nutzen.
- Gebaut wird ausschließlich über die Kommandozeile – in einem **normalen**
  PowerShell-/CMD-Fenster, **nicht** in der "Developer Command Prompt for VS"
  (die zieht wieder VS' eigenes, zu altes MSBuild).

Voraussetzung dafür: **.NET 10 SDK** installiert (`dotnet --list-sdks` prüfen,
sonst von [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
herunterladen) und die MAUI-Workload:

```
dotnet workload install maui
```

---

## Android-APK lokal bauen

```
cd E:\Git\Wawi\SchnullerkettchenMobile
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

Fertige Datei liegt danach hier:

```
bin\Release\net10.0-android\publish\de.schnullerkettchen.wawi.mobile-Signed.apk
```

Die APK deckt alle gängigen Prozessorarchitekturen ab (`arm64-v8a` für so gut
wie jedes moderne Handy, `armeabi-v7a` für ältere 32-Bit-Geräte, `x86_64` für
Emulatoren) und ist automatisch mit einem generierten Ad-hoc-Key signiert –
für internes Sideloading reicht das ohne eigenen Keystore.

**Installation:** Datei aufs Handy kopieren (z. B. per USB-Kabel, Mail oder
Cloud-Speicher), dort öffnen, "Installation aus unbekannter Quelle" für die
verwendete App (Dateimanager/Mail-App) erlauben, installieren. Ist bereits
eine ältere, anders signierte Version installiert (z. B. eine Debug-Version
aus Visual Studio), muss diese vorher deinstalliert werden – sonst meldet
Android einen Signatur-Konflikt.

### Einmalige Voraussetzung: Android SDK API 36

.NET 10 verlangt standardmäßig Android-API-Ebene 36. Fehlt die lokal, kommt
beim ersten Build `error XA5207`. Fix (**PowerShell als Administrator**,
da nach `C:\Program Files (x86)\...` geschrieben wird):

```
dotnet build -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk" -p:AcceptAndroidSDKLicenses=true
```

Danach normal (nicht als Admin) weiterbauen.

---

## iOS-IPA über GitHub Actions bauen

Läuft komplett in der Cloud auf einem Mac-Runner – kein eigener Mac nötig.
Voraussetzung ist die einmalige Einrichtung aus `IOS-CI-ANLEITUNG.md`
(Zertifikat, Provisioning-Profil, GitHub Secrets/Variables).

1. GitHub-Repo öffnen → Tab **Actions**.
2. Workflow **"iOS Build (Ad-Hoc IPA)"** auswählen (oder **"iOS Build
   (TestFlight)"** für die TestFlight-Variante, siehe unten) → **Run
   workflow** → Branch `main` → **Run workflow**.
   *(Läuft auch automatisch bei jedem Push auf `main`, der `.cs`/`.xaml`-
   Dateien oder das `.csproj` ändert.)*
3. Warten, bis der Lauf grün ist (ca. 5–10 Minuten).
4. Auf der Lauf-Seite ganz nach unten scrollen → Abschnitt **Artifacts** →
   `SchnullerkettchenMobile-iOS` herunterladen (eine `.zip`, die die `.ipa`
   enthält).

   Startet der Download im Browser nicht: Seite neu laden und erneut
   versuchen, einen anderen Browser probieren, oder Download-/Pop-up-Blocker
   für github.com kurz deaktivieren.

### Installation der Ad-Hoc-IPA

- **.ipa** bei [diawi.com](https://www.diawi.com) hochladen → Link bzw.
  QR-Code auf dem iPhone in Safari öffnen → App installiert sich direkt.
- Voraussetzung: Das iPhone muss vorher als Testgerät im Apple-Developer-
  Portal registriert worden sein (UDID unter *Devices*, siehe
  `IOS-CI-ANLEITUNG.md`, Schritt 2) – sonst verweigert iOS die Installation
  trotz gültiger Signatur.

### Alternative: TestFlight statt Ad-Hoc

Der Workflow **"iOS Build (TestFlight)"** lädt die App direkt zu App Store
Connect hoch. Vorteil: keine Geräte-UDIDs registrieren, Tester werden per
E-Mail eingeladen und installieren ganz normal über die offizielle
TestFlight-App. Braucht eine separate App-Store-Provisioning-Profil +
App-Store-Connect-API-Key-Einrichtung (`IOS-CI-ANLEITUNG.md`, Schritt 3b/4b).

---

## Troubleshooting: aufgetretene Fehler & Lösungen

Diese Fehler sind in der Praxis tatsächlich aufgetreten – falls sie wieder
auftauchen (z. B. nach einem SDK-Update), hier die bereits gefundenen Fixes:

| Fehler | Ursache | Fix |
|---|---|---|
| `NETSDK1139: Zielplattformbezeichner "android" wurde nicht erkannt` | .NET-10-SDK nicht installiert/aktiv (`dotnet build` greift auf ein älteres SDK, z. B. 9.0.x) | `dotnet --list-sdks` prüfen, ggf. .NET 10 SDK installieren |
| `NETSDK1209: ... Visual Studio 18.0 oder höher` | Build lief über Visual Studio (IDE oder Developer Command Prompt) statt reiner CLI | Nur `dotnet build`/`dotnet publish` im normalen Terminal verwenden, siehe oben |
| `error XA5207: android.jar für API-Ebene 36 wurde nicht gefunden` | Android SDK hat Plattform 36 nicht installiert | `dotnet build -t:InstallAndroidDependencies ...` (siehe oben), **als Administrator** |
| `The Android SDK license agreements were not accepted` | Lizenzen nicht akzeptiert | `-p:AcceptAndroidSDKLicenses=true` an den obigen Befehl anhängen |
| `Failure to move Android component ... UnauthorizedAccessException` | Kein Schreibzugriff auf `C:\Program Files (x86)\...` | PowerShell **als Administrator** öffnen |
| `CS0103: Der Name "ImageResizer" ist im aktuellen Kontext nicht vorhanden` | Datei fehlte im `Util`-Ordner | `Util/ImageResizer.cs` wiederhergestellt (3 JPEG-Größen per SkiaSharp) |
| `CS0266: DateTime? kann nicht implizit in DateTime konvertiert werden` (VacationPage) | Seit .NET MAUI 10 ist `DatePicker.Date` nullable (`DateTime?`) | Mit `?? vorherigerWert` absichern |
| `security: MAC verification failed during PKCS12 import (wrong password?)` | OpenSSL 3.x nutzt standardmäßig SHA-256 als MAC-Algorithmus für `.p12` – macOS' `security`-Tool versteht nur SHA-1 | Beim Export: `-certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg SHA1` |
| `iOS code signing key '...' not found in keychain` | GitHub-Variable `IOS_CODESIGN_IDENTITY` passt nicht exakt zum String im Keychain (z. B. "Apple Distribution" vs. "iPhone Distribution" – abhängig vom gewählten Zertifikatstyp) | Diagnose-Ausgabe im Workflow-Log ("Verfügbare Codesigning-Identitäten im Keychain:") 1:1 übernehmen |
| `The specified iOS provisioning profile '...' could not be found` | `CodesignProvision` enthielt die Bundle-ID statt des Profil-**Namens** | Diagnose-Ausgabe im Log ("Name des importierten Provisioning-Profils:") in die Variable `IOS_PROVISION_PROFILE_NAME` übernehmen |
| `Project bundle identifier '...' does not match specified provisioning profile '...'` | Das Profil wurde im Apple-Portal versehentlich mit der falschen App-ID erstellt | Profil im Portal löschen, mit korrekter App-ID (`de.schnullerkettchen.wawi.mobile`) neu erstellen, neu als Base64 in `IOS_PROVISION_PROFILE_BASE64` hinterlegen |
| `openssl req: subject name is expected to be in the format ...` | `-subj`-Argument in Git Bash/MSYS falsch interpretiert (Pfad-Mangling) | Interaktiven `openssl req` **ohne** `-subj` verwenden (Prompts einzeln beantworten) |

---

## Nach jeder Änderung: nicht vergessen

```
git add -A
git commit -m "..."
git push
```

Erst danach sieht GitHub Actions die Änderungen und kann eine neue Version
bauen.
