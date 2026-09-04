# iOS-Build per GitHub Actions – Einrichtung

Es gibt zwei Workflows, für zwei unterschiedliche Verteilwege - Apples Xcode-Toolchain läuft in beiden Fällen zwingend auf einem (von GitHub bereitgestellten) macOS-Runner, das lässt sich nicht umgehen, wohl aber outsourcen:

- **`.github/workflows/ios-build.yml`** – baut eine **Ad-Hoc-IPA** zum manuellen Verteilen (z. B. über diawi.com). Nur auf vorher per UDID registrierten Geräten installierbar (max. 100/Jahr).
- **`.github/workflows/ios-testflight.yml`** – baut die App und lädt sie direkt zu **TestFlight** hoch ("iTunes" heißt heute App Store Connect, früher iTunes Connect). Für rein internen Einsatz meist die praktischere Wahl: keine Geräte-UDIDs registrieren, Tester werden einfach per E-Mail eingeladen, Installation läuft über die offizielle TestFlight-App, Update-Benachrichtigung inklusive.

Beide brauchen dasselbe Zertifikat (Schritt 1), aber unterschiedliche Provisioning-Profile. Schritt 1–2 und 5 gelten für beide Workflows; Schritt 3/4 gibt es separat für Ad-Hoc und TestFlight.

## Voraussetzung: Apple Developer Program

Ohne kostenpflichtige Mitgliedschaft (99 $/Jahr, [developer.apple.com](https://developer.apple.com/programs/)) geht es nicht – nur damit lassen sich Ad-Hoc-Zertifikate und -Profile erzeugen.

## Schritt 1: Zertifikat erzeugen (auch ohne Mac möglich)

Normalerweise übernimmt ein Mac (Keychain Access) das Erzeugen des Signierschlüssels. Es geht aber genauso mit OpenSSL – z. B. direkt hier im Projektordner unter Windows (WSL/Git Bash) oder Linux:

```bash
openssl genrsa -out ios_distribution.key 2048
openssl req -new -key ios_distribution.key -out ios_distribution.csr
```

Der zweite Befehl fragt interaktiv die Angaben ab – nur ausfüllen:
- **Country Name**: `DE`
- **Common Name**: euer Name, z. B. `Rainer Popp`
- **Email Address**: eure E-Mail
- Alle anderen Felder (State, Organization, ...) einfach mit Enter überspringen

**Hinweis für Git Bash unter Windows:** Bewusst *ohne* `-subj "/emailAddress=.../CN=.../C=DE"` beschrieben, weil Git Bash (MSYS) Argumente, die mit `/` beginnen, automatisch als Windows-Pfad umdeutet und dabei verstümmelt – führt zu Fehlern wie *"subject name is expected to be in the format ..."* mit einem `C:/Program Files/Git/...`-Pfad in der Fehlermeldung. Der interaktive Weg oben umgeht das Problem komplett. Wer unbedingt `-subj` nutzen will: mit vorangestelltem `MSYS_NO_PATHCONV=1` ausführen, oder den Wert mit doppeltem Slash beginnen lassen (`"//emailAddress=..."`).

1. Im [Apple Developer Portal](https://developer.apple.com/account/resources/certificates/list) → Certificates → "+" → **Apple Distribution** auswählen.
2. Die erzeugte `ios_distribution.csr` hochladen, Zertifikat herunterladen (`.cer`).
3. In ein `.p12` umwandeln (enthält Zertifikat + privaten Schlüssel, braucht ein Passwort – das später als Secret `IOS_CERTIFICATE_PASSWORD` hinterlegt wird):

```bash
openssl x509 -in ios_distribution.cer -inform DER -out ios_distribution.pem -outform PEM
openssl pkcs12 -export -inkey ios_distribution.key -in ios_distribution.pem -out ios_distribution.p12 -password pass:DEIN_PASSWORT -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg SHA1
```

**Wichtig:** Die Flags `-certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg SHA1` sind zwingend nötig. OpenSSL 3.x (Standard unter Git Bash/aktuellem Linux) verschlüsselt `.p12`-Dateien sonst mit SHA-256 als MAC-Algorithmus – Apples `security`-Tool auf dem macOS-Runner versteht das nicht und meldet beim Import fälschlich *"MAC verification failed ... (wrong password?)"*, obwohl das Passwort stimmt. Mit den Legacy-Flags oben tritt der Fehler nicht auf.

## Schritt 2: Testgeräte registrieren (nur für Ad-Hoc, für TestFlight überspringen)

Im Portal unter *Devices* → "+" → Gerätename + UDID jedes iPhones eintragen, auf dem die App später installiert werden soll. Die UDID findet man z. B. über iTunes/Finder (Gerät anschließen, auf die Seriennummer klicken) oder Apple Configurator. Für TestFlight (Schritt 3b) entfällt das komplett – Apple kümmert sich dort selbst um die Geräteverteilung.

## Schritt 3a: App-ID und Ad-Hoc-Profil anlegen (für `ios-build.yml`)

1. *Identifiers* → "+" → App-ID mit Bundle-ID `de.schnullerkettchen.wawi.mobile` (siehe `SchnullerkettchenMobile.csproj`, `ApplicationId`) anlegen, falls noch nicht vorhanden.
2. *Profiles* → "+" → **Ad Hoc** → die App-ID, das eben erzeugte Zertifikat und die registrierten Geräte auswählen → Profil herunterladen (`.mobileprovision`).
3. Profilnamen notieren (steht im Portal neben dem Profil) – der wird gleich als Repository-Variable gebraucht.

## Schritt 4a: Ad-Hoc-Werte in GitHub hinterlegen

Zertifikat und Profil müssen als Base64-Text in die Secrets, da GitHub nur Text-Werte speichert:

```bash
base64 -i ios_distribution.p12 | tr -d '\n' > cert_base64.txt
base64 -i profil.mobileprovision | tr -d '\n' > profile_base64.txt
```

Im Repository unter **Settings → Secrets and variables → Actions**:

**Secrets** (New repository secret):

| Name | Wert |
|---|---|
| `IOS_CERTIFICATE_P12_BASE64` | Inhalt von `cert_base64.txt` |
| `IOS_CERTIFICATE_PASSWORD` | Passwort aus Schritt 1 |
| `IOS_PROVISION_PROFILE_BASE64` | Inhalt von `profile_base64.txt` |
| `IOS_KEYCHAIN_PASSWORD` | Ein beliebiges neues Passwort (nur für den temporären CI-Keychain, frei wählbar) |

**Variables** (Tab daneben, "Variables" statt "Secrets" – unkritische Werte, kein Geheimnis):

| Name | Wert |
|---|---|
| `IOS_CODESIGN_IDENTITY` | `Apple Distribution: <Dein Name> (<TEAM-ID>)` – exakter Name steht in Keychain Access bzw. im Portal unter *Certificates* |
| `IOS_PROVISION_PROFILE_NAME` | Der in Schritt 3a notierte Profilname |

## Schritt 3b: App-Store-Profil und API-Key anlegen (für `ios-testflight.yml`)

**Provisioning-Profil:**

1. Gleiche App-ID wie oben (falls noch nicht angelegt, siehe Schritt 3a.1).
2. *Profiles* → "+" → **App Store** (nicht Ad Hoc!) → die App-ID und das Zertifikat aus Schritt 1 auswählen → Profil herunterladen.
3. Profilnamen notieren.

**App Store Connect API-Key** (getrennt vom Developer-Portal, unter [appstoreconnect.apple.com](https://appstoreconnect.apple.com/access/integrations/api)):

1. *Integrations* → *App Store Connect API* → "+" → Rolle **App Manager** → Key erzeugen.
2. Die `.p8`-Datei **sofort herunterladen** – das geht nur einmal, bei Verlust muss ein neuer Key erzeugt werden.
3. `Key ID` und `Issuer ID` notieren (stehen direkt auf der Seite).

## Schritt 4b: TestFlight-Werte in GitHub hinterlegen

```bash
base64 -i appstore_profil.mobileprovision | tr -d '\n' > appstore_profile_base64.txt
```

**Secrets:**

| Name | Wert |
|---|---|
| `IOS_APPSTORE_PROVISION_PROFILE_BASE64` | Inhalt von `appstore_profile_base64.txt` |
| `APPSTORE_API_PRIVATE_KEY` | Kompletter Inhalt der `.p8`-Datei aus Schritt 3b |

`IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD` und `IOS_KEYCHAIN_PASSWORD` aus Schritt 4a werden wiederverwendet (gleiches Zertifikat).

**Variables:**

| Name | Wert |
|---|---|
| `IOS_APPSTORE_PROVISION_PROFILE_NAME` | Der in Schritt 3b notierte Profilname |
| `APPSTORE_API_KEY_ID` | Key ID aus Schritt 3b |
| `APPSTORE_ISSUER_ID` | Issuer ID aus Schritt 3b |

`IOS_CODESIGN_IDENTITY` aus Schritt 4a wird wiederverwendet.

**TestFlight-Build-Nummer:** Der Workflow setzt die Build-Nummer automatisch auf die GitHub-Actions-Laufnummer (`-p:ApplicationVersion=${{ github.run_number }}`) – ohne eindeutig steigende Nummer lehnt TestFlight jeden zweiten Upload mit *"duplicate build number"* ab, das übernimmt der Workflow also automatisch.

## Schritt 5: Projekt zu GitHub pushen

`SchnullerkettchenMobile` ist aktuell noch kein Git-Repository. Im Projektordner:

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/RainerPopp1982/SchnullerkettchenMobile.git
git push -u origin main
```

(Repository auf GitHub vorher unter github.com/new anlegen, **privat**, da im Code DB-/FTP-Zugangsdaten stehen – siehe README.md.)

## Schritt 6: Workflow starten

Nach dem Push laufen beide Workflows automatisch an (bei jedem Push auf `main`). Manuell geht es jederzeit über den Tab **Actions** → *iOS Build (Ad-Hoc IPA)* bzw. *iOS Build (TestFlight)* → **Run workflow**. Braucht ihr nur einen der beiden Wege, die jeweils andere `.yml`-Datei einfach aus `.github/workflows/` löschen.

## Schritt 7: App installieren

**Ad-Hoc (`ios-build.yml`):** Nach erfolgreichem Lauf im Actions-Tab auf den Lauf klicken → unter **Artifacts** die `SchnullerkettchenMobile-iOS`-Datei herunterladen (enthält die `.ipa`). Installieren auf einem der registrierten Geräte:

- **Apple Configurator 2** (kostenlos, Mac erforderlich): iPhone per Kabel verbinden, IPA per Drag & Drop installieren.
- **Ohne Mac:** einen kostenlosen Ad-Hoc-Verteildienst wie [diawi.com](https://www.diawi.com/) nutzen – IPA dort hochladen, Link/QR-Code direkt am iPhone in Safari öffnen, App installiert sich darüber (Gerät muss im Profil aus Schritt 2 registriert sein).

**TestFlight (`ios-testflight.yml`):** Der Upload läuft automatisch am Ende des Workflows. Danach in [App Store Connect](https://appstoreconnect.apple.com/) → *TestFlight* → *Interne Tests* (oder *Externe Tests*) die gewünschten Personen per E-Mail als Tester hinzufügen. Sie bekommen eine Einladung, installieren die **TestFlight**-App aus dem App Store und darüber dann eure App – inklusive automatischer Update-Benachrichtigung bei jedem neuen Upload. Ein neuer Build braucht bis zu 15–30 Minuten, bis er nach dem Upload für Tester bereitsteht (Apples automatische Verarbeitung).

## Wichtig: Kosten der macOS-Runner

Das Repository muss **privat** bleiben (siehe oben). Private Repos haben ein monatliches Freikontingent an Actions-Minuten, macOS-Runner verbrauchen davon aber das **10-fache** der tatsächlichen Laufzeit (10 Minuten realer Build ≈ 100 Minuten vom Kontingent). Bei gelegentlichen Builds (ein paar Mal im Monat) bleibt das i. d. R. im kostenlosen Rahmen; bei sehr häufigen Builds können zusätzliche Kosten entstehen (github.com/settings/billing zeigt den aktuellen Verbrauch).

## Hinweis zur Zuverlässigkeit

Dieser Workflow wurde nach bekannten, gängigen Mustern für .NET-MAUI-iOS-CI-Builds erstellt, aber in dieser Umgebung nicht selbst gegen einen echten Apple-Account getestet (kein Zugriff auf Xcode/macOS). Beim ersten echten Lauf kann es sein, dass Feinheiten nachjustiert werden müssen – am wahrscheinlichsten die exakte Schreibweise von `IOS_CODESIGN_IDENTITY`/`IOS_PROVISION_PROFILE_NAME` oder die Xcode-Version im Workflow (aktuell `15.4`, ggf. an die dann unterstützte .NET-8-MAUI-Workload-Version anpassen). Bei einer Fehlermeldung im Actions-Log gerne den Log-Ausschnitt schicken, dann schaue ich mir das gezielt an.

### Fehler NETSDK1202 ("workload ... is out of support")

Das Projekt wurde ursprünglich mit `net8.0-android`/`net8.0-ios` angelegt. Microsofts .NET-MAUI-Support-Policy (aka.ms/maui-support-policy) hat diese Workloads inzwischen als "out of support" eingestuft – aktuelle .NET-SDKs (der macOS-Runner bringt mittlerweile .NET 10 mit) lehnen den Build dann mit `NETSDK1202` komplett ab, oft zusammen mit einem `NU1101`-Fehler zu einem nicht auffindbaren Runtime-Paket. Behoben durch Anheben von `SchnullerkettchenMobile.csproj` und beiden Workflow-Dateien auf `net10.0-android`/`net10.0-ios` (aktuelle LTS-Version). **Wichtig:** Das betrifft nicht nur GitHub Actions – auch ein lokaler Build in Visual Studio schlägt fehl, sobald dort ebenfalls ein neueres .NET-SDK installiert ist; Visual Studio braucht dafür die **.NET 10 SDK**. Sollte dieser Fehler in einigen Monaten wieder auftauchen (Microsoft stuft TFMs regelmäßig neu ein), einfach die TargetFrameworks auf die dann aktuelle .NET-Version anheben – gleiches Prinzip.

### Node-20-Warnung/-Fehler ("Node 20 actions are deprecated")

GitHub stellt die Actions-Runtime schrittweise von Node 20 auf Node 24 um (Node20 wird am 23. September 2026 komplett entfernt). Der Workflow nutzt bereits die aktuellen, Node-24-fähigen Versionen (`actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`, `maxim-lobanov/setup-xcode@v1.7.0`). Taucht die Meldung trotzdem wieder auf, hat vermutlich eine der Actions inzwischen eine neuere Version veröffentlicht, oder GitHub hat die Übergangsfrist weiter verschoben – dann einfach den aktuellen Log-Ausschnitt schicken, ich prüfe die passenden Versionen erneut.
