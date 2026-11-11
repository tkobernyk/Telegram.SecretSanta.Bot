using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.DAL.Entities;
using SecretSanta.Domain.Mappings;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Tests;

internal sealed class ParticipantService : IParticipantRepository
{
	private readonly IParticipantRepository _repository;

	public ParticipantService(IParticipantRepository repository)
	{
		_repository = repository;
	}

	public async Task<ParticipantDto?> GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken = default)
	{
		var entity = await _repository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);
		return entity == null ? null : ParticipantMapper.Map(entity);
	}

	public async Task<ParticipantDto> CreateAsync(ParticipantDto participant, CancellationToken cancellationToken = default)
	{
		await _repository.CreateAsync(ParticipantMapper.Map(participant), cancellationToken);
		return participant;
	}

	public async Task<ParticipantDto> UpdateAsync(ParticipantDto participant, CancellationToken cancellationToken = default)
	{
		await _repository.UpdateAsync(ParticipantMapper.Map(participant), cancellationToken);
		return participant;
	}

	Task<Participant?> IParticipantRepository.GetByTelegramUserIdAsync(string eventId, string telegramUserId, CancellationToken cancellationToken)
		=> _repository.GetByTelegramUserIdAsync(eventId, telegramUserId, cancellationToken);

	Task IParticipantRepository.CreateAsync(Participant participant, CancellationToken cancellationToken)
		=> _repository.CreateAsync(participant, cancellationToken);

	Task IParticipantRepository.UpdateAsync(Participant participant, CancellationToken cancellationToken)
		=> _repository.UpdateAsync(participant, cancellationToken);

	Task<IEnumerable<Participant>> IParticipantRepository.GetAllByEventAsync(string eventId, CancellationToken cancellationToken)
		=> _repository.GetAllByEventAsync(eventId, cancellationToken);
}