using System;
public class HexMapPosition : Tuple<int, int> {
    public HexMapPosition(int x, int y) : base(x, y) { }
    public int x { get { return Item1; } }
    public int y { get { return Item2; } }
}