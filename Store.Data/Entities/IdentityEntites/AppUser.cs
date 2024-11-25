﻿using Microsoft.AspNetCore.Identity;

namespace Store.Data.Entities.IdentityEntites
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public Address Address { get; set; }
    }
}
