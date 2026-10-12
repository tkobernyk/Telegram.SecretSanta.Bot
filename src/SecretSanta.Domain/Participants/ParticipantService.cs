using SecretSanta.DAL.Abstractions;
using SecretSanta.Domain.Models;
using SecretSanta.Domain.Mappings;

namespace SecretSanta.Domain.Participants;


public interface IParticipantService
{
	Task<ParticipantDto?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default);
	Task<ParticipantDto> CreateAsync(ParticipantDto participant, CancellationToken cancellationToken = default);
}

public class ParticipantService : IParticipantService
{
	private readonly IParticipantRepository _repository;

	public ParticipantService(IParticipantRepository repository)
	{
		_repository = repository;
	}

	public async Task<ParticipantDto?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var entity = await _repository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		if (entity == null) return null;
		return ParticipantMapper.Map(entity);
	}

	public async Task<ParticipantDto> CreateAsync(ParticipantDto participant, CancellationToken cancellationToken = default)
	{
		var entity = ParticipantMapper.Map(participant);
		await _repository.CreateAsync(entity, cancellationToken);
		return participant;
	}
}
