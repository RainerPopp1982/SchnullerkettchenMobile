namespace SchnullerkettchenMobile.Util.BrotherQl;

// IP-Adresse/Hostname des Brother QL-820NWB(C) im lokalen Netzwerk. Eine feste IP (DHCP-
// Reservierung am Router) wird dringend empfohlen - ändert sich die IP, findet die App den
// Drucker sonst nicht mehr.
public static class PrinterConfig
{
    public const string Host = "192.168.0.187";
    public const int Port = 9100; // Standard-Rohdruckport, an dem der QL-820NWB im Netzwerk lauscht
}
