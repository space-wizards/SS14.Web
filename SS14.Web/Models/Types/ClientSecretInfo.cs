using System;

namespace SS14.Web.Models.Types;

public sealed record ClientSecretInfo(int Id, DateTime CreatedOn, string Description, bool Legacy);
