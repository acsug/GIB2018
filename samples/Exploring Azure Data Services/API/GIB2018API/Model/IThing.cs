using System;

namespace GIB2018API.Model
{
    public interface IThing
    {
        string Type { get; }

        string Id { get; set; }

        bool? Deleted { get; set; }

        DateTimeOffset? CreatedAt { get; set; }

        DateTimeOffset? UpdatedAt { get; set; }
    }
}
