using System.ComponentModel.DataAnnotations;
using LMS.API.Enums;

namespace LMS.API.DTOs;

public class MoveRequestDto
{
    [Required(ErrorMessage = "Direction is required.")]
    [EnumDataType(typeof(MoveDirection), ErrorMessage = "Direction must be Up or Down.")]
    public MoveDirection? Direction { get; set; }
}