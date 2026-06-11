namespace RetradeBE.Models.DTOs
{
    public class ManageRoleDto
    {
        public string AccountId { get; set; } = null!;

        public List<RoleDto> AllRoles { get; set; } = new();

        public List<RoleDto> AssignedRole { get; set; } = new();
    }

    public class RoleDto
    {
        public int RoleId { get; set; }

        public string Name { get; set; } = null!;
    }
}
