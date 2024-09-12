using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Models;

[Keyless]
public class MapPosition
{
    public int BeatmapId { get; set; }
    
    [Column("rownumber")]
    public int RowNumber { get; set; }
}