using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Domain.Helpers;

public static class InlineCalendarKeyboard
{
	private static readonly string[] MonthNames =
	[
		"Січень",
		"Лютий",
		"Березень",
		"Квітень",
		"Травень",
		"Червень",
		"Липень",
		"Серпень",
		"Вересень",
		"Жовтень",
		"Листопад",
		"Грудень"
	];

	public static InlineKeyboardMarkup Create(DateOnly month, DateOnly minDate)
	{
		var monthStart = new DateOnly(month.Year, month.Month, 1);
		var monthEnd = monthStart.AddMonths(1).AddDays(-1);
		var previousMonth = monthStart.AddMonths(-1);
		var nextMonth = monthStart.AddMonths(1);

		var rows = new List<InlineKeyboardButton[]>();
		rows.Add([InlineKeyboardButton.WithCallbackData($"📅 {MonthNames[monthStart.Month - 1]} {monthStart.Year}", "calendar:noop")]);
		rows.Add(
		[
			InlineKeyboardButton.WithCallbackData("◀️", $"calendar:month:{previousMonth:yyyy-MM}"),
			InlineKeyboardButton.WithCallbackData("Сьогодні", $"calendar:month:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM}"),
			InlineKeyboardButton.WithCallbackData("▶️", $"calendar:month:{nextMonth:yyyy-MM}")
		]);
		rows.Add(
		[
			InlineKeyboardButton.WithCallbackData("Пн", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Вт", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Ср", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Чт", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Пт", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Сб", "calendar:noop"),
			InlineKeyboardButton.WithCallbackData("Нд", "calendar:noop")
		]);

		var offset = ((int)monthStart.DayOfWeek + 6) % 7;
		var day = 1;
		while (day <= monthEnd.Day)
		{
			var row = new List<InlineKeyboardButton>();
			for (var column = 0; column < 7; column++)
			{
				if ((rows.Count == 3 && column < offset) || day > monthEnd.Day)
				{
					row.Add(InlineKeyboardButton.WithCallbackData(" ", "calendar:noop"));
					continue;
				}

				var date = new DateOnly(monthStart.Year, monthStart.Month, day);
				if (date < minDate)
				{
					row.Add(InlineKeyboardButton.WithCallbackData("·", "calendar:noop"));
				}
				else
				{
					row.Add(InlineKeyboardButton.WithCallbackData(day.ToString("00"), $"calendar:date:{date:yyyy-MM-dd}"));
				}

				day++;
			}

			rows.Add([.. row]);
		}

		rows.Add([InlineKeyboardButton.WithCallbackData("❌ Скасувати", "calendar:cancel")]);
		return new InlineKeyboardMarkup(rows);
	}
}
