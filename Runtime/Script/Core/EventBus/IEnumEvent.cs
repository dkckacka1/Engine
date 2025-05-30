namespace Engine.Core.EventBus
{
    public interface IEnumEvent<T>
    {
        public void OnEnumChange(T type);
    }
}