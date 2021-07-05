public class HexPositionPair {
    public HexPositionPair(UnityMapHex hex, HexMapPosition position) {
        this.hex = hex;
        this.position = position;
    }

    public void setHex(UnityMapHex hex) {
        this.hex = hex;
    }

    public HexPositionPair(HexPositionPair original) : this(original.hex, original.position) { }
    public UnityMapHex hex { get; private set; }
    public HexMapPosition position { get; private set; }
}