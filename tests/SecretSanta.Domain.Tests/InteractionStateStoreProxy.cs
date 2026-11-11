using SecretSanta.Domain.Handlers.Common;
using SecretSanta.Domain.Models;

namespace SecretSanta.Domain.Tests;

internal static class InteractionStateStoreProxy
{
	public static void StartConversation(string telegramUserId, ConversationState state) => InteractionStateStore.StartConversation(telegramUserId, state);
	public static bool TryGetConversation(string telegramUserId, out ConversationState? state) => InteractionStateStore.TryGetConversation(telegramUserId, out state);
	public static void UpdateConversation(string telegramUserId, ConversationState state) => InteractionStateStore.UpdateConversation(telegramUserId, state);
	public static void EndConversation(string telegramUserId) => InteractionStateStore.EndConversation(telegramUserId);
	public static void SetActiveEventId(string telegramUserId, string eventId) => InteractionStateStore.SetActiveEventId(telegramUserId, eventId);
	public static bool TryGetActiveEventId(string telegramUserId, out string eventId) => InteractionStateStore.TryGetActiveEventId(telegramUserId, out eventId);
	public static void ClearActiveEventId(string telegramUserId) => InteractionStateStore.ClearActiveEventId(telegramUserId);
}