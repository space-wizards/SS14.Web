using System;

namespace SS14.Web.Models.Types;

public sealed record ClientSecretInfo(int Id, DateTimeOffset CreatedOn, string Description, bool Legacy);
