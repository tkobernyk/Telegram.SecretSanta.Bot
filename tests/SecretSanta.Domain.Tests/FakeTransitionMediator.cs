using MediatR;
using SecretSanta.DAL.Abstractions.Repositories;
using SecretSanta.Domain.Abstractions;
using SecretSanta.Domain.Handlers.Commands.Workflow;
using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Services.Workflow;
using SecretSanta.Domain.Services.Workflow.Rules;

namespace SecretSanta.Domain.Tests;

internal sealed class FakeTransitionMediator : IMediator
{
	private readonly ApplyEventTransitionHandler _transitionHandler;

	public FakeTransitionMediator(IEventRepository eventRepository)
	{
		var rules = new IEventStateTransitionRule[]
		{
			new RegistrationVoteSessionStartedRule(),
			new BudgetConfirmationVoteSessionStartedRule(),
			new BudgetDiscussionVoteSessionStartedRule(),
			new VotingBudgetConsensusReachedRule(),
			new VotingBudgetConsensusNotReachedRule(),
			new VotingDraftStartedRule(),
			new BudgetDiscussionDraftStartedRule(),
			new DraftDraftStartedRule(),
			new DraftGiftProgressUpdatedRule(),
			new PreparingGiftProgressUpdatedRule(),
			new DraftAllGiftsReadyRule(),
			new PreparingAllGiftsReadyRule(),
			new AwaitingDeliveryDeliveryInfoProvidedRule()
		};
		var ruleFactory = new EventStateTransitionRuleFactory(rules);
		var stateMachine = new EventStateMachine(ruleFactory);
		_transitionHandler = new ApplyEventTransitionHandler(eventRepository, stateMachine);
	}

	public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		if (request is ApplyEventTransitionRequest transitionRequest)
		{
			return HandleTransition<TResponse>(transitionRequest, cancellationToken);
		}

		throw new InvalidOperationException($"Unsupported request type in fake mediator: {request.GetType().Name}");
	}

	public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
		where TRequest : IRequest
	{
		throw new NotSupportedException($"Unsupported non-generic request type in fake mediator: {request?.GetType().Name}");
	}

	public Task<object?> Send(object request, CancellationToken cancellationToken = default)
	{
		if (request is ApplyEventTransitionRequest transitionRequest)
		{
			return HandleTransitionObject(transitionRequest, cancellationToken);
		}

		throw new InvalidOperationException($"Unsupported request type in fake mediator: {request.GetType().Name}");
	}

	public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException($"Unsupported stream request type in fake mediator: {request.GetType().Name}");
	}

	public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException($"Unsupported stream request type in fake mediator: {request.GetType().Name}");
	}

	public Task Publish(object notification, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
		where TNotification : INotification
	{
		return Task.CompletedTask;
	}

	private async Task<TResponse> HandleTransition<TResponse>(ApplyEventTransitionRequest transitionRequest, CancellationToken cancellationToken)
	{
		var response = await _transitionHandler.Handle(transitionRequest, cancellationToken);
		if (response is TResponse typed)
		{
			return typed;
		}

		throw new InvalidOperationException($"Transition response cannot be cast to {typeof(TResponse).Name}");
	}

	private async Task<object?> HandleTransitionObject(ApplyEventTransitionRequest transitionRequest, CancellationToken cancellationToken)
	{
		return await _transitionHandler.Handle(transitionRequest, cancellationToken);
	}
}
