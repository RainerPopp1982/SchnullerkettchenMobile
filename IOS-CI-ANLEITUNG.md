# iOS-Build per GitHub Actions – Einrichtung

Dieser Workflow (`.github/workflows/ios-build.yml`) baut bei jedem Push auf `main` (oder manuell per Knopfdruck) eine installierbare **Ad-Hoc-IPA** – ganz ohne eigenen Mac. Gebaut wird auf einem von GitHub bereitgestellten macOS-Runner; Apples Xcode-Toolchain läuft zwingend auf macOS, das lässt sich nicht umgehen, wohl aber outsourcen.

Eine Ad-Hoc-IPA lässt sich nur auf vorher registrierten Geräten installieren (max. 100 pro Jahr) – für internes Testen ist das genau richtig. Für eine spätere Verteilung über TestFlight oder den App Store braucht es einen leicht abgewandelten Export-Typ; sagt Bescheid, falls das gebraucht wird.

## Voraussetzung: Apple Developer Program

Ohne kostenpflichtige Mitgliedschaft (99 $/Jahr, [developer.apple.com](https://developer.apple.com/programs/)) geht es nicht – nur damit lassen sich Ad-Hoc-Zertifikate und -Profile erzeugen.

## Schritt 1: Zertifikat erzeugen (auch ohne Mac möglich)

Normalerweise übernimmt ein Mac (Keychain Access) das Erzeugen des Signierschlüssels. Es geht aber genauso mit OpenSSL – z. B. direkt hier im Projektordner unter Windows (WSL/Git Bash) oder Linux:

```bash
openssl genrsa -out ios_distribution.key 2048
openssl req -new -key ios_distribution.key -out ios_distribution.csr -subj "/emailAddress=shopping@rainer-popp.de, CN=Rainer Popp, C=DE"
```

1. Im [Apple Developer Portal](https://developer.apple.com/account/resources/certificates/list) → Certificates → "+" → **Apple Distribution** auswählen.
2. Die erzeugte `ios_distribution.csr` hochladen, Zertifikat herunterladen (`.cer`).
3. In ein `.p12` umwandeln (enthält Zertifikat + privaten Schlüssel, braucht ein Passwort – das später als Secret `IOS_CERTIFICATE_PASSWORD` hinterlegt wird):

```bash
openssl x509 -in ios_distribution.cer -inform DER -out ios_distribution.pem -outform PEM
openssl pkcs12 -export -inkey ios_distribution.key -in ios_distribution.pem -out ios_distribution.p12 -password pass:DEIN_PASSWORT
```

## Schritt 2: Testgeräte registrieren

Im Portal unter *Devices* → "+" → Gerätename + UDID jedes iPhones eintragen, auf dem die App später installiert werden soll. Die UDID findet man z. B. über iTunes/Finder (Gerät anschließen, auf die Seriennummer klicken) oder Apple Configurator.

## Schritt 3: App-ID und Ad-Hoc-Profil anlegen

1. *Identifiers* → "+" → App-ID mit Bundle-ID `de.schnullerkettchen.wawi.mobile` (siehe `SchnullerkettchenMobile.csproj`, `ApplicationId`) anlegen, falls noch nicht vorhanden.
2. *Profiles* → "+" → **Ad Hoc** → die App-ID, das eben erzeugte Zertifikat und die registrierten Geräte auswählen → Profil herunterladen (`.mobileprovision`).
3. Profilnamen notieren (steht im Portal neben dem Profil) – der wird gleich als Repository-Variable gebraucht.

## Schritt 4: Werte in GitHub hinterlegen

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
| `IOS_PROVISION_PROFILE_NAME` | Der in Schritt 3 notierte Profilname |

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

Nach dem Push läuft der Workflow automatisch an. Manuell geht es jederzeit über den Tab **Actions** → *iOS Build (Ad-Hoc IPA)* → **Run workflow**.

## Schritt 7: IPA installieren

Nach erfolgreichem Lauf: im Actions-Tab auf den Lauf klicken → unter **Artifacts** die `SchnullerkettchenMobile-iOS`-Datei herunterladen (enthält die `.ipa`). Installieren auf einem der registrierten Geräte:

- **Apple Configurator 2** (kostenlos, Mac erforderlich): iPhone per Kabel verbinden, IPA per Drag & Drop installieren.
- **Ohne Mac:** einen kostenlosen Ad-Hoc-Verteildienst wie [diawi.com](https://www.diawi.com/) nutzen – IPA dort hochladen, Link/QR-Code direkt am iPhone in Safari öffnen, App installiert sich darüber (Gerät muss im Profil aus Schritt 2 registriert sein).

## Wichtig: Kosten der macOS-Runner

Das Repository muss **privat** bleiben (siehe oben). Private Repos haben ein monatliches Freikontingent an Actions-Minuten, macOS-Runner verbrauchen davon aber das **10-fache** der tatsächlichen Laufzeit (10 Minuten realer Build ≈ 100 Minuten vom Kontingent). Bei gelegentlichen Builds (ein paar Mal im Monat) bleibt das i. d. R. im kostenlosen Rahmen; bei sehr häufigen Builds können zusätzliche Kosten entstehen (github.com/settings/billing zeigt den aktuellen Verbrauch).

## Hinweis zur Zuverlässigkeit

Dieser Workflow wurde nach bekannten, gängigen Mustern für .NET-MAUI-iOS-CI-Builds erstellt, aber in dieser Umgebung nicht selbst gegen einen echten Apple-Account getestet (kein Zugriff auf Xcode/macOS). Beim ersten echten Lauf kann es sein, dass Feinheiten nachjustiert werden müssen – am wahrscheinlichsten die exakte Schreibweise von `IOS_CODESIGN_IDENTITY`/`IOS_PROVISION_PROFILE_NAME` oder die Xcode-Version im Workflow (aktuell `15.4`, ggf. an die dann unterstützte .NET-8-MAUI-Workload-Version anpassen). Bei einer Fehlermeldung im Actions-Log gerne den Log-Ausschnitt schicken, dann schaue ich mir das gezielt an.
