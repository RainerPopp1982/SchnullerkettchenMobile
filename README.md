# Schnullerkettchen Mobile (SchnullerkettchenMobile)

.NET MAUI App (Android + iOS) für unterwegs. Version 1 deckt zwei Bereiche ab:

1. **Bestellungen** – offene Bestellungen (`bestellnr.bestaetigt` = 0 oder 1) oder alle, plus Suche nach E-Mail/Adresse. Bestellung öffnen zeigt Kunde, (abweichende) Lieferadresse, Bestelldaten und Artikelpositionen inkl. Bild. Reine Ansicht, keine Bearbeitung. Dazu: Adressetikett-Druck (siehe unten).
2. **Artikelsuche** – Artikel suchen, öffnen und bearbeiten (`dekoartikel` + `dekoartikel_varianten`), inkl. Artikelbildern (`tbl_images`): Anzeigen, Hinzufügen (Kamera/Galerie), Ersetzen und Löschen.
3. **Urlaubseinstellung** – Urlaubsmodus des Shops an/aus, Zeitraum, Lieferzeit während des Urlaubs, Urlaubstext und Urlaubsbild (siehe eigener Abschnitt unten).

## Voraussetzungen zum Bauen

- Visual Studio 2022 (17.8+) mit der Workload **".NET Multi-platform App UI development"**.
- Für Android: Android-SDK wird von Visual Studio automatisch mitverwaltet.
- **Für iOS: ein Mac mit Xcode ist zwingend erforderlich** (entweder direkt oder als "Pair to Mac" von Windows aus). Ohne Mac kann das `net8.0-ios`-Target nicht gebaut werden – das Projekt lässt sich aber unabhängig davon ganz normal für Android bauen und testen.

Projekt einfach über `SchnullerkettchenMobile.sln` in Visual Studio öffnen, Zielgerät (Android-Emulator oder Gerät) auswählen, Starten.

## Datenbankzugriff

Die App verbindet sich **direkt per MySQL** (gleicher Server/gleiche Zugangsdaten wie die Desktop-App, siehe `Data/DatabaseConfig.cs`) – kein eigenes API-Backend. Das wurde bewusst so entschieden, um schnell eine funktionierende v1 zu haben.

**Wichtiger Hinweis:** Dadurch stecken die DB-Zugangsdaten im App-Paket. Bei einer Android-/iOS-App ist das leichter extrahierbar (Dekompilierung) als bei einer Windows-EXE. Für den produktiven Einsatz über den eigenen Kreis hinaus sollte das perspektivisch durch ein schlankes API-Backend mit eigener Authentifizierung ersetzt werden.

## Bilder-Upload

Wie am Desktop (`Programme/Artikel/ImageUpload.cs`) wird jedes Bild in **3 Größen** (250/500/1200 px, per SkiaSharp auf die jeweilige Box skaliert, nie vergrößert, JPEG-Qualität 85) erzeugt und per FTP hochgeladen (`Data/ImageUploadService.cs`, `Data/FtpConfig.cs` – gleicher FTP-Server/gleiche Zugangsdaten wie `FTPUpload/UploadFiles.cs`, aber mit der asynchronen FluentFTP-API `AsyncFtpClient`).

- **Hinzufügen:** "Bild hinzufügen" auf der Artikelseite → Kamera oder Galerie auswählen → Upload in allen 3 Größen → neue Zeile in `tbl_images` (Dateiname `{ArtikelID}_{Position}.jpg`, nächste freie Position).
- **Ersetzen:** Bestehendes Bild antippen → "Ersetzen" → neue Datei wird unter demselben Dateinamen hochgeladen (überschreibt alle 3 Größen). Wie am Desktop bewusst **ohne** DB-Änderung, da sich der Dateiname nicht ändert.
- **Löschen:** Bestehendes Bild antippen → "Löschen" → Datei wird in allen 3 Größen von der FTP entfernt UND die Zeile in `tbl_images` gelöscht (das kann die Desktop-App bei einzelnen Bildern bisher nicht).

Für Kamera/Galerie fordert die App beim ersten Zugriff die nötigen Berechtigungen an (Android: Kamera + Medien/Speicher; iOS: Kamera + Fotomediathek, siehe `Platforms/*/AndroidManifest.xml` bzw. `Info.plist`).

## Urlaubseinstellung

Entspricht der Desktop-Seite `Programme/Content/Urlaub.xaml` (Shopkonfiguration). Anders als die übrigen Bereiche greift diese Funktion auf eine **separate Datenbank/eigenen DB-User** zu (`Data/ShopConfigConfig.cs`, DB-User "SchnullerkettchenConfig" – genau wie am Desktop `DBConnection("SchnullerkettchenConfig")`), nicht auf die normale Shop-Datenbank.

- Tabelle `Urlaub` (eine einzige Konfigurationszeile): Urlaubsmodus an/aus, Zeitraum (von/bis), Datum "wieder verfügbar ab", Lieferzeit während des Urlaubs (Tage von/bis), Urlaubstext.
- Urlaubsbild: Kamera/Galerie-Auswahl wie beim Artikel-Bilder-Upload, aber **ohne Größenanpassung** – die Originaldatei wird 1:1 in den Layout-Ordner der Website hochgeladen (`media/layout/`, gleicher FTP-Server wie die Produktbilder, siehe `Data/FtpConfig.cs`).
- Speichern schreibt die Werte parametrisiert in die DB (der Desktop baut die UPDATE-Anweisung per String-Interpolation zusammen – hier bewusst sicherer gelöst, funktional identisch).

## Bewusste Einschränkungen v1

- **Beschreibung als Rohtext:** Das Beschreibungsfeld ist ein einfaches Texteingabefeld für den HTML-Quelltext, kein WYSIWYG-Editor wie in der Desktop-App.
- **Bestellungen nur lesend:** Es gibt keine Bearbeitung/Status-Änderung von Bestellungen in dieser App, nur Anzeige, Suche und Etikettendruck.
- **Silikonring/Glöckchen/Karabiner/Zusatztexte/Versand** u.ä. Spezialfelder aus `dekoartikel` sind (wie mittlerweile auch in der Desktop-App) nicht enthalten, um die Bearbeitungsmaske schlank zu halten.
- Der genaue Status-Wertebereich von `bestellnr.bestaetigt` (0/1 = "offen" laut Vorgabe, was 2/3/... bedeuten ist app-seitig nicht bekannt) wird nur als Rohzahl angezeigt, nicht in Klartext übersetzt.

## Adressetikett-Druck – wichtiger Unterschied zur Desktop-App

Die Desktop-App druckt Etiketten direkt und automatisch über Brothers **b-PAC-SDK** (COM/ActiveX) auf den Brother QL-820NWB. **b-PAC gibt es nur für Windows** – das lässt sich auf Android/iOS nicht nachbauen. Brother bietet zwar ein eigenes mobiles Print-SDK für Android/iOS an, das erfordert aber eine Registrierung und einen Download über Brothers Entwicklerportal sowie eine native Anbindung – beides war im Rahmen dieser Änderung nicht möglich (kein Zugriff auf das Portal).

**Stattdessen:** Der Button "Adressetikett drucken" erzeugt ein exakt bemessenes Etikettenbild (62×29 mm, 203 dpi – wie die `62x29mmLand.lbx`-Vorlage am Desktop) und öffnet das Betriebssystem-Share-Sheet. Von dort lässt es sich direkt an die kostenlose **"Brother iPrint&Label"-App** (Play Store/App Store) weitergeben, die den QL-820NWB kennt und darüber drucken kann – ein Tap mehr als am Desktop, aber funktioniert ohne proprietäres SDK.

Falls ihr Zugangsdaten fürs Brother-Entwicklerportal habt bzw. das SDK bereits heruntergeladen ist: sagt Bescheid, dann kann ich eine native Anbindung nachrüsten, die direkt (ohne Share-Sheet-Umweg) druckt.

## Projektstruktur

```
Data/           MySQL-Zugriff (OrdersRepository, ArticlesRepository, VacationRepository), ImageUploadService (FTP-Upload), FtpConfig, DatabaseConfig, ShopConfigConfig
Models/         Einfache Datenklassen (dekoartikel, dekoartikel_varianten, tbl_images, Bestellungen, Urlaub)
Util/           SeoUrlHelper (SEO-Slugs), SafeReader (toleranter DB-Zugriff), LabelPrinter (Etikettenbild), ImageResizer (3 Bildgrößen)
Views/          Seiten (MainMenuPage, OrdersPage, OrderDetailPage, ArticleSearchPage, ArticleDetailPage, VariantEditPage, VacationPage)
Platforms/      Android- und iOS-spezifischer Bootstrap-Code
Resources/      Icons, Splash Screen, Farben/Styles (mit Dark-Mode-Unterstützung)
```

## Bekannt: nicht in dieser Sandbox gebaut

Dieses Projekt wurde ohne lokal installiertes .NET-/MAUI-SDK erstellt (kein Internetzugriff zum Nachinstallieren in dieser Umgebung) und daher **nicht** hier kompiliert/getestet. Der Code folgt exakt dem Standard-Aufbau eines `dotnet new maui`-Projekts – ein erster Build in Visual Studio ist trotzdem der wichtigste nächste Schritt, um eventuelle Tippfehler zu finden.
