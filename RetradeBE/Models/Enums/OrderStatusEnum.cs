namespace RetradeBE.Models.Enums
{
    public enum OrderStatusEnum
    {
        AwaitingPayment,
        Pending,
        Confirmed,
        Shipping,
        Delivered,
        DeliveryFailed,
        Completed,
        Returned,
        Cancelled
    }
}
