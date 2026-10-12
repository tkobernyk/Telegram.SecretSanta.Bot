using Riok.Mapperly.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.DAL.Entities;

namespace SecretSanta.Domain.Mappings;

[Mapper]
public static partial class ParticipantMapper
{
	public static partial ParticipantDto Map(Participant source);
	public static partial Participant Map(ParticipantDto source);
}
