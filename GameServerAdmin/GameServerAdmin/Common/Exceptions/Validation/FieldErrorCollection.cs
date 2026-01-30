using GameServerAdmin.Common.Responses;

namespace GameServerAdmin.Common.Exceptions.Validation
{
    public class FieldErrorCollection : Dictionary<string, List<string>>
    {
        public void AddError(string field, string message)
        {
            if(!TryGetValue(field, out var errors))
            {
                errors = new List<string>();
                this[field] = errors;
            }
            errors.Add(message);
        }

    }
}
