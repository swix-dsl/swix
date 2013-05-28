namespace SimpleWixDsl.Swix.Model
{
    public class ComponentGroup
    {
        public ComponentGroup(string id)
        {
            Id = id;
        }

        public string Id { get; private set; }
    }
}