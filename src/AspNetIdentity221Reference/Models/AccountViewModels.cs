using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace AspNetIdentity221Reference.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        //[Display(Name = "Correo electrónico")]
        //[Display(ResourceType = typeof(LocalizedStrings), Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        //[Display(Name = "Contraseña")]
        //[Display(ResourceType = typeof(LocalizedStrings), Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        //[Display(Name = "Recordarme?")]
        //[Display(ResourceType = typeof(LocalizedStrings), Name = "Account_RememberMe")]
        public bool RememberMe { get; set; }
    }
}