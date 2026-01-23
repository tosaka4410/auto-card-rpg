public static class FatigueSystem
{
    public static int GetFatigueDamage(int turn)
    {
        if (turn < 5) return 0;
        return turn - 4; // 5ターン目から1,2,3...
    }
}
