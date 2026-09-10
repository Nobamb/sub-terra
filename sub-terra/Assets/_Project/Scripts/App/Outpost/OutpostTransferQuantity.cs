namespace SubTerra.App.Outpost
{
    /// <summary>
    /// 보관함 입출고 요청 수량을 실제 이동 가능량으로 맞춘다.
    /// 요청이 보유량보다 크면 남은 전부를 옮기고, 보유가 없으면 0을 반환한다.
    /// </summary>
    public static class OutpostTransferQuantity
    {
        public static int ClampToAvailable(int requested, int available)
        {
            if (requested <= 0 || available <= 0)
            {
                return 0;
            }

            return requested < available ? requested : available;
        }
    }
}
