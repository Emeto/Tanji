namespace Tanji.Services
{
    public interface IHaltable
    {
        void Halt();
        void Restore();
    }
}