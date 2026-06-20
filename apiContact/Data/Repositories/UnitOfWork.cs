namespace apiContact.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        public IUserRepository    Users    { get; }
        public IRoomRepository    Rooms    { get; }
        public IMessageRepository Messages { get; }
        public IAuditRepository   Audit    { get; }

        public UnitOfWork(
            IUserRepository    users,
            IRoomRepository    rooms,
            IMessageRepository messages,
            IAuditRepository   audit)
        {
            Users    = users;
            Rooms    = rooms;
            Messages = messages;
            Audit    = audit;
        }
    }
}
