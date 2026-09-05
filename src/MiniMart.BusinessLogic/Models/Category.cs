using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class Category
{
    public const int NameMaxLength = 100;

    private string _name = string.Empty;

    public int CategoryId { get; private set; }

    public string Name
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("Category name is required.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > NameMaxLength)
            {
                throw new BusinessRuleException($"Category name cannot exceed {NameMaxLength} characters.");
            }

            _name = trimmed;
        }
    }

    public Category(string name)
    {
        Name = name;
    }

    public static Category FromDatabase(int categoryId, string name)
    {
        return new Category(name) { CategoryId = categoryId };
    }

    public void Rename(string newName) => Name = newName;

    public void AssignIdentity(int categoryId) => CategoryId = categoryId;

    public override string ToString() => Name;
}
