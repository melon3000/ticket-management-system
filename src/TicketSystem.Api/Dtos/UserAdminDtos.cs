using System.ComponentModel.DataAnnotations;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Dtos;

/// <summary>The new role of a user. Only User, Support and Admin can be given to a person.</summary>
public class ChangeRoleRequest
{
    /// <example>Support</example>
    [Required, EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; set; }
}
