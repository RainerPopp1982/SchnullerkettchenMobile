namespace SchnullerkettchenMobile.Util.BrotherQl;

// Medientyp-Byte für den "media/quality"-Befehl (ESC i z) - Werte laut Brothers "Software
// Developer's Manual - Raster Command Reference QL-800/810W/820NWB" bzw. gegengeprüft mit der
// quelloffenen Bibliothek brother_ql (github.com/pklaus/brother_ql, MIT-Lizenz).
public enum QlFormFactor : byte
{
    Endlos = 0x0A,
    Stanzetikett = 0x0B,
}

// Beschreibt eine Etiketten-/Bandgröße, wie sie im QL-820NWB eingelegt sein kann. Alle Dot-Werte
// gelten für 300 dpi (Standardauflösung, 720 Dots / 90 Bytes Kopfbreite - fix für die gesamte
// QL-8xx-Serie, unabhängig von der eingelegten Rolle).
public sealed class QlLabel
{
    public string Bezeichnung { get; }
    public QlFormFactor FormFactor { get; }

    // Physische Bandgröße in mm, wie sie der Drucker im "media/quality"-Befehl erwartet.
    // Bei Endlosband ist Laenge immer 0 (die Länge ergibt sich erst aus dem Inhalt).
    public (int Breite, int Laenge) TapeGroesseMm { get; }

    // Druckbarer Bereich in Dots (@300dpi) - das ist die Bildgröße, die RenderAddressLabel
    // erzeugt (ggf. seitenvertauscht, wenn hochkant zugeführt wird, siehe BrotherQlPrinter).
    public (int Breite, int Laenge) DotsDruckbar { get; }

    // Abstand in Dots, den der Inhalt von der rechten Kante des 720 Dots breiten Druckkopfs
    // braucht, damit er auf dem schmaleren Etikett zentriert landet.
    public int RandRechtsDots { get; }

    // Zusätzlicher Vorschub in Dots - bei Endlosband bzw. einigen kleinen Etiketten nötig,
    // damit der Sensor das Bandende sauber erkennt.
    public int VorschubDots { get; }

    public QlLabel(string bezeichnung, QlFormFactor formFactor, (int Breite, int Laenge) tapeGroesseMm,
        (int Breite, int Laenge) dotsDruckbar, int randRechtsDots, int vorschubDots = 0)
    {
        Bezeichnung = bezeichnung;
        FormFactor = formFactor;
        TapeGroesseMm = tapeGroesseMm;
        DotsDruckbar = dotsDruckbar;
        RandRechtsDots = randRechtsDots;
        VorschubDots = vorschubDots;
    }

    public override string ToString() => Bezeichnung;
}

// Gängige Etiketten-/Bandgrößen für den QL-820NWB(C). Wird eine andere Rolle verwendet, hier
// einfach einen weiteren Eintrag ergänzen (Werte aus Brothers Datenblatt zur jeweiligen DK-
// Nummer, oder aus brother_ql/labels.py übernehmen).
public static class QlMediaCatalog
{
    // 62x29mm Stanzetikett - das ist die Größe, die auch die Desktop-App ("62x29mm.lbx" /
    // "62x29mmLand.lbx") und die bisherige Bild-Variante (Util/LabelPrinter.cs) für
    // Adressetiketten verwenden. Deshalb hier als Standard gesetzt, passend zur tatsächlich
    // im Betrieb eingelegten Rolle.
    public static readonly QlLabel Adressetikett_62x29 =
        new("62x29mm Stanzetikett", QlFormFactor.Stanzetikett, (62, 29), (696, 271), randRechtsDots: 12);

    // 62x100mm Stanzetikett - z.B. für größere Versandetiketten.
    public static readonly QlLabel Versandetikett_62x100 =
        new("62x100mm Stanzetikett", QlFormFactor.Stanzetikett, (62, 100), (696, 1109), randRechtsDots: 12);

    // DK-11201: Brothers eigenes Standard-Adressetikett (29x90mm, hochkant zugeführt).
    public static readonly QlLabel DK11201_Adressetikett_29x90 =
        new("DK-11201 Adressetikett 29x90mm", QlFormFactor.Stanzetikett, (29, 90), (306, 991), randRechtsDots: 6);

    // 62mm Endlosband (kein fester Etikettenabstand, Länge richtet sich nach dem Inhalt).
    public static readonly QlLabel Endlosband_62mm =
        new("62mm Endlosband", QlFormFactor.Endlos, (62, 0), (696, 0), randRechtsDots: 12, vorschubDots: 35);
}
