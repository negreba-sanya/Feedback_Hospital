using MySql.Data.MySqlClient;
using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using System.Configuration;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

namespace Feedback
{
	class Program
	{
		private static string connStr_users;


		private static TelegramBotClient client_User;
		private static TelegramBotClient client_Group;
		private static TelegramBotClient client_Chat;
		// URL чата(для наших специалистов) для перехода в приватный диалог для решения проблемы
		private static string client_Chat_URL;
		// название бота для корректной обработки команд
		private static string groupBotName;

		// клавиатуры
		public static InlineKeyboardMarkup keyboard_accept_in_group = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Принять", "accept")
								}
							});
		public static ReplyKeyboardMarkup keyboard_for_workers_in_dialog = new ReplyKeyboardMarkup
		{
			Keyboard = new[] {
								new[]
												{
													new KeyboardButton("Заблокировать человека")
												},
												new[]
												{
													new KeyboardButton("Завершить диалог")
												},
											},
			ResizeKeyboard = true
		};
		public static ReplyKeyboardMarkup keyboard_for_users_in_dialog = new ReplyKeyboardMarkup
		{
			Keyboard = new[] {
												new[]
												{
													new KeyboardButton("Завершить диалог")
												},
											},
			ResizeKeyboard = true
		};
		public static InlineKeyboardMarkup keyboard_URL_in_group = new InlineKeyboardMarkup(new[]
					{
								new []
								{
									InlineKeyboardButton.WithUrl("Перейти к чату", "http://t.me/KKB_chat_bot")
								}
							});
		public static InlineKeyboardMarkup keyboard_registration_for_workers = new InlineKeyboardMarkup(new[]
					  {
								new []
								{
									InlineKeyboardButton.WithCallbackData("Зарегистрироваться", "registration")
								}
							});
		public static InlineKeyboardMarkup keyboard_cancel = new InlineKeyboardMarkup(new[]
					{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Отмена", "cancel")
								}
							});


		static void Main(string[] args)
		{
			connStr_users = ConfigurationManager.AppSettings.Get("connStr_users");
			SqlConnection connection = new SqlConnection(connStr_users);

			// для ссылок в чат, чтобы внутри был переход. по ссылке не переходил
			client_Chat_URL = ConfigurationManager.AppSettings.Get("chatBot_name");
			// для того, чтобы отрезать реальную команду, которая будет по формате /info@botName (если добавлять в список доступных команд)
			groupBotName = ConfigurationManager.AppSettings.Get("groupBot_name");

			// бот для клиентов
			client_User = new TelegramBotClient(ConfigurationManager.AppSettings.Get("token_User"));
			client_User.StartReceiving();

			// бот для беседы (добавлен в группу поддержки)
			client_Group = new TelegramBotClient(ConfigurationManager.AppSettings.Get("token_Group"));
			client_Group.StartReceiving();

			// бот для общения с клиентом
			client_Chat = new TelegramBotClient(ConfigurationManager.AppSettings.Get("token_Chat"));
			client_Chat.StartReceiving();

			// обработчики события при полученнии сообщений 
			client_User.OnMessage += UserBot;
			client_Group.OnMessage += GroupBot;
			client_Chat.OnMessage += ChatBot;


			// обработчик нажатия inline-кнопок в боте для клиентов
			client_User.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) =>
			{
				string reply_text;
				var message = ev.CallbackQuery.Message;

				// выбор нажатой кнопки
				switch (ev.CallbackQuery.Data)
				{
					case "num1":

						TopicAppeal(1, ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

						break;

					case "num2":

						TopicAppeal(2, ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

						break;
					case "num3":

						TopicAppeal(3, ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

						break;
					case "address":
						await client_User.SendLocationAsync(ev.CallbackQuery.From.Id, 56.026619f, 92.913801f);
						reply_text = GetInfo(ev.CallbackQuery.Data);
						await client_User.SendTextMessageAsync(ev.CallbackQuery.From.Id, reply_text.ToString());
						await client_User.DeleteMessageAsync(ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);
						break;
					case "timesheet":
					case "contacts":
					case "services":
						reply_text = GetInfo(ev.CallbackQuery.Data);
						await client_User.SendTextMessageAsync(ev.CallbackQuery.From.Id, reply_text.ToString());
						await client_User.DeleteMessageAsync(ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);
						break;

					case "one":
					case "two":
					case "three":
					case "four":
					case "five":
						await client_User.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Спасибо за ваш отзыв!");
						await client_User.DeleteMessageAsync(ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);
						break;
				}

			};

			// обработчик нажатия inline-кнопок в боте для беседы
			client_Group.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) =>
			{
				var message = ev.CallbackQuery.Message;
				string i = ev.CallbackQuery.Message.Text;

				// выбор нажатой кнопки
				switch (ev.CallbackQuery.Data)
				{
					case "accept":
					

							if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT telegram_id FROM Employee WHERE telegram_id = " + ev.CallbackQuery.From.Id + " AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')THEN 1 ELSE 0 END AS BIT)")).Equals(false))
							{
							await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT id_group FROM Group WHERE telegram_id IS NOT NULL")), "Здравствуйте, " + ev.CallbackQuery.From.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_URL_in_group);
							await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Здравствуйте, " + ev.CallbackQuery.From.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
							}
							else
							{
								if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT id_employee FROM Employee WHERE telegram_id = " + ev.CallbackQuery.From.Id + ")THEN 1 ELSE 0 END AS BIT)")).Equals(false))
								{
									await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE telegram_id IS NOT NULL")), "Вы ещё не прошли авторизацию. Для этого перейдите по ссылке " + client_Chat_URL, replyMarkup: keyboard_URL_in_group);

								}
								else
								{

									if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT id_appeal FROM Appeal WHERE id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + ev.CallbackQuery.From.Id + ") AND status = 1)THEN 1 ELSE 0 END AS BIT)")).Equals(false))
									{
										if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT id_appeal FROM Appeal WHERE id_employee = 0 AND status = 0)THEN 1 ELSE 0 END AS BIT)")).Equals(false))
										{
											Regex regex = new Regex(@"#[0-9]+");
											MatchCollection matches = regex.Matches(ev.CallbackQuery.Message.Text);

											if (matches.Count == 1)
											{
												foreach (Match match in matches)
												{// находим id обращения и обновляем данные
													ExecuteNonQuery("UPDATE Appeal SET id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + ev.CallbackQuery.From.Id + "), status = 1 WHERE id_appeal = " + Convert.ToInt32(match.Value.Replace("#", "")) + "");

													await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Можете приступать к работе с человеком по обращению №" + Convert.ToInt32(match.Value.Replace("#", ""))+ "\n\n(" + ev.CallbackQuery.Message.Text+")", replyMarkup: keyboard_for_workers_in_dialog);
													await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Patient.telegram_id FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE id_appeal = " + Convert.ToInt32(match.Value.Replace("#", "")) + "")), "Наш сотрудник - " + ev.CallbackQuery.From.FirstName + " поможет решить вашу проблему!", replyMarkup: keyboard_for_users_in_dialog);

													await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE telegram_id IS NOT NULL")), "Запрос №"+ Convert.ToInt32(match.Value.Replace("#", "")) + " принят сотрудником - " + ev.CallbackQuery.From.FirstName + "." + "\n\n(" + ev.CallbackQuery.Message.Text+")", replyMarkup: keyboard_URL_in_group);
													await client_Group.DeleteMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE telegram_id IS NOT NULL")), ev.CallbackQuery.Message.MessageId);
												}
											}
										}
                                        else
                                        {
											await client_Group.DeleteMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE telegram_id IS NOT NULL")), ev.CallbackQuery.Message.MessageId);
										}
									}
									else
									{
										await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE telegram_id IS NOT NULL")), ev.CallbackQuery.From.FirstName + ", завершите диалог с клиентом, чтобы принять новое обращение.");
									}

								}

							}
				
						break;
				}

			};

			// обработчик нажатия inline-кнопок в боте для общения с клиентом
			client_Chat.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) =>
			{
				var message = ev.CallbackQuery.Message;

				// выбор нажатой кнопки
				switch (ev.CallbackQuery.Data)
				{
					case "registration":
						await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, ev.CallbackQuery.From.FirstName + ", введите логин и пароль, которые вам предоставили. Укажите их через пробел (Например: \"login password\")", replyMarkup: keyboard_cancel);
						break;
					case "cancel":
						await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Действие отменено.");
						await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Вы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
						break;
				}
			};

			Console.ReadKey();

			client_User.StopReceiving();
			client_Group.StopReceiving();
			client_Chat.StopReceiving();

		}


		private static async void ChatBot(object sender, MessageEventArgs e)
		{
			// отсутсвует функция остановки диалога, сообщения просто отсылаются в бот для клиентов
			var msg = e.Message;

			switch (msg.Text)
			{
				case "/start":
					if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") THEN 1 ELSE 0 END AS BIT)")).Equals(false))
					{

						if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT telegram_id FROM Employee WHERE telegram_id = " + msg.Chat.Id + " AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')THEN 1 ELSE 0 END AS BIT)")).Equals(false))
						{
							await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
						}
						else
						{
							await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nПримите запрос в вашей беседе, чтобы начать диалог с клиентом.");
						}
					}
                    else
                    {
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nЭтот бот предназначен только для сотрудников.");
					}
					break;

				default:
					if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") THEN 1 ELSE 0 END AS BIT)")).Equals(false))
					{
						if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT telegram_id FROM Employee WHERE telegram_id = " + msg.Chat.Id + " AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')THEN 1 ELSE 0 END AS BIT)")).Equals(false))
						{
							try
							{
								string[] log_pas = new string[2];
								log_pas = msg.Text.Split(' ');

								if (log_pas[0].Length > 40)
								{
									log_pas[0] = log_pas[0].Substring(0, 40);
								}
								if (log_pas[1].Length > 40)
								{
									log_pas[1] = log_pas[1].Substring(0, 40);
								}


								if (Convert.ToInt32(ExecuteScalar("SELECT id_employee FROM Employee WHERE login = '" + GetHash(log_pas[0]) + "' AND password = '" + GetHash(log_pas[1]) + "' AND telegram_id IS NULL")).Equals(1))
								{
									SqlConnection connection = new SqlConnection(connStr_users);
									connection.Open();

									string query = "UPDATE Employee SET telegram_id = @telegram_id WHERE login = '" + GetHash(log_pas[0]) + "' AND password = '" + GetHash(log_pas[1]) + "'";
									SqlCommand command_ni = new SqlCommand();
									command_ni.CommandText = query;
									command_ni.Connection = connection;
									command_ni.Parameters.AddWithValue("@telegram_id", msg.Chat.Id);
									command_ni.ExecuteNonQuery();

									connection.Close();



									await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Регистрация прошла успешно!");
									await client_Chat.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
									await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Примите запрос в вашей беседе, чтобы начать диалог с клиентом.");
								}
								else
								{
									await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Введенный логин или пароль неверен.");
									await client_Chat.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
								}
							}
							catch (Exception error)
							{
								Console.WriteLine(error.Message);
								await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Введенный логин или пароль неверен.");
								await client_Chat.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
							}
						}
						else
						{


							if (!Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT id_appeal FROM Appeal WHERE id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND status = 1)THEN 1 ELSE 0 END AS BIT)")).Equals(false))
							{
								try
								{
									SqlConnection connection = new SqlConnection(connStr_users);
									connection.Open();

									string query = "INSERT INTO [Employee message history] (employee_message_id, date, text) VALUES (@employee_message_id, @date, @text)";
									SqlCommand command = new SqlCommand();
									command.CommandText = query;
									command.Connection = connection;
									command.Parameters.AddWithValue("@employee_message_id", Convert.ToInt32(ExecuteScalar("SELECT id_appeal FROM Appeal WHERE id_employee = (SELECT id_employee FROM Appeal WHERE id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND status = 1) AND id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND status = 1")));
									command.Parameters.AddWithValue("@text", msg.Text);
									command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
									command.ExecuteNonQuery();

									connection.Close();
								}
								catch
								{

								}

								if (msg.Text == "" || msg.Text == null)
								{
									try
									{
										await client_User.SendStickerAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM Patient WHERE id_patient = (SELECT id_patient FROM Appeal WHERE id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND status = 1)")), msg.Sticker.FileId);
									}
									catch
									{
										await client_Chat.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
										await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Вы можете отправлять только текстовые сообщения, эмодзи и стикеры.");
									}
								}
								else
								{
									await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM Patient WHERE id_patient = (SELECT id_patient FROM Appeal WHERE id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND status = 1)")), "<b>" + msg.Chat.FirstName + "</b>" + "\n" + msg.Text, ParseMode.Html);
								}
							}
							else
							{
								await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Примите запрос в вашей беседе, чтобы начать диалог с клиентом.");
							}
						}
					}
                    else
                    {
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nЭтот бот предназначен только для сотрудников.");
					}

					break;
				case "Завершить диалог":
					if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT telegram_id FROM Employee WHERE telegram_id = " + msg.Chat.Id + " AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')THEN 1 ELSE 0 END AS BIT)")).Equals(true))
					{
						var keyboardUser = new ReplyKeyboardMarkup
						{
							Keyboard = new[] {
												new[]
												{
													new KeyboardButton("Задать вопрос"),
													new KeyboardButton("Информация")
												},
											},
							ResizeKeyboard = true
						};

						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Patient.telegram_id FROM Appeal JOIN Patient ON Patient.id_patient = Appeal.id_patient WHERE Appeal.id_employee= (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "Сотрудник завершил диалог");
						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Patient.telegram_id FROM Appeal JOIN Patient ON Patient.id_patient = Appeal.id_patient WHERE Appeal.id_employee= (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "Выберите команду:", replyMarkup: keyboardUser);
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Диалог завершен", replyMarkup: new ReplyKeyboardRemove());

						ExecuteNonQuery("UPDATE Appeal SET Appeal.status = 2 WHERE Appeal.id_appeal = (SELECT * FROM(SELECT Appeal.id_appeal FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_employee = (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1) AS t)");
					}
                    else
                    {
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
					}
					break;
				case "Заблокировать человека":
					if (Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS(SELECT telegram_id FROM Employee WHERE telegram_id = " + msg.Chat.Id + " AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')THEN 1 ELSE 0 END AS BIT)")).Equals(true))
					{
						var keyboardUs = new ReplyKeyboardMarkup
						{
							Keyboard = new[] {
												new[]
												{
													new KeyboardButton("Задать вопрос"),
													new KeyboardButton("Информация")
												},
											},
							ResizeKeyboard = true
						};

						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Patient.telegram_id FROM Appeal JOIN Patient ON Patient.id_patient = Appeal.id_patient WHERE Appeal.id_employee= (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "Сотрудник завершил диалог");
						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Patient.telegram_id FROM Appeal JOIN Patient ON Patient.id_patient = Appeal.id_patient WHERE Appeal.id_employee= (SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "Выберите команду:", replyMarkup: keyboardUs);
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Диалог завершен", replyMarkup: new ReplyKeyboardRemove());
						ExecuteNonQuery("UPDATE Patient SET Patient.blocked = 1 WHERE Patient.id_patient = (SELECT * FROM (SELECT Appeal.id_patient FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_employee = (SELECT * FROM(SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AS t1) AND Appeal.status = 1) AS t)");
						ExecuteNonQuery("UPDATE Appeal SET Appeal.status = 3 WHERE Appeal.id_appeal = (SELECT * FROM (SELECT Appeal.id_appeal FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_employee = (SELECT * FROM(SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") AS t1) AND Appeal.status = 1) AS t)");
						

					}
                    else
                    {
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
					}
					break;

			}
		}

		private static async void GroupBot(object sender, MessageEventArgs e)
		{
			var msg = e.Message;
			var keyboard = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Принять", "accept")
								}
							});

			if (msg.Text == "/info@" + groupBotName)
			{
				await client_Group.SendTextMessageAsync(msg.Chat.Id, "Я буду присылать вам запросы клиентов из главного бота. Для принятия запроса нажмите на кнопку. Принять запрос может только один человек.");
			}
			if (msg.Text == "/appeal@" + groupBotName) 
			{
				SqlConnection connection = new SqlConnection(connStr_users);	
					connection.Open();
					SqlCommand command = new SqlCommand("SELECT Appeal.id_appeal, Patient.surname, Patient.name, Patient.patronymic, Appeal.description FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE status = 0", connection);
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						client_Group.SendTextMessageAsync(msg.Chat.Id, "#" + Convert.ToInt32(reader[0]) + "\nФИО: " + reader[1].ToString() + " " + reader[2].ToString() + "" + reader[3].ToString() + "\n" + "Тема: Качество обслуживания" + "\n" + "Описание: " + reader[4].ToString(), replyMarkup: keyboard);
					}
					connection.Close();


					if (Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM(SELECT Appeal.id_appeal FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE status = 0) AS T")).Equals(0))
					{
						client_Group.SendTextMessageAsync(msg.Chat.Id, "Необработанных обращений нет");
					}
			 }
		}

		private static async void UserBot(object sender, MessageEventArgs e)
		{
			// локальная переменная для получения информации о сообщениях
			var msg = e.Message;
			// reply-кнопки при первом запуске бота
			var keyboardStart = new ReplyKeyboardMarkup
			{
				Keyboard = new[] {
												new[]
												{
													new KeyboardButton("Задать вопрос"),
													//new KeyboardButton("Отзыв"),
													new KeyboardButton("Информация")
												},
											},
				ResizeKeyboard = true
			};

			// inline-кнопки при нажатии на "Техподдержка"
			var keyboardQuestion = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData(ExecuteScalar("SELECT name FROM [Subject Appeal] WHERE id_subject_appeal = 1"), "num1")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData(ExecuteScalar("SELECT name FROM [Subject Appeal] WHERE id_subject_appeal = 2"), "num2")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData(ExecuteScalar("SELECT name FROM [Subject Appeal] WHERE id_subject_appeal = 3"), "num3")
								}
							});

			// inline-кнопки при нажатии на "Отзыв"
			var keyboardFeedback = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("1 - не удовлетворен", "one")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("2 - удовлетворен", "two")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("3 - нормально", "three")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("4 - хорошо", "four")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("5 - отлично", "five")
								}
							});

			// inline-кнопки при нажатии на "Информация"
			var keyboardInformation = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Адрес", "address")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("Режим работы", "timesheet")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("Контакты", "contacts")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("Платные услуги", "services")
								}
							});

			switch (msg.Text)
			{
				case "Задать вопрос":
					if (Check_Patient(msg))
					{
						if (Convert.ToBoolean(ExecuteScalar("SELECT blocked FROM Patient WHERE telegram_id = " + msg.Chat.Id + "")).Equals(false))
						{
							if (Convert.ToInt32(DateTime.Now.Hour) >= Convert.ToInt32(ConfigurationManager.AppSettings.Get("time_begin")) && Convert.ToInt32(DateTime.Now.Hour) < Convert.ToInt32(ConfigurationManager.AppSettings.Get("time_end")))
							{
								await client_User.SendTextMessageAsync(msg.Chat.Id, "Выберите тему обращения:", replyMarkup: keyboardQuestion);
							}
							else
							{
								await client_User.SendTextMessageAsync(msg.Chat.Id, "Вы можете обратиться в техподдержку с " + ConfigurationManager.AppSettings.Get("time_begin") + ":00 до " + ConfigurationManager.AppSettings.Get("time_end") + ":00.");
							}
						}
						else
						{
							await client_User.SendTextMessageAsync(msg.Chat.Id, "Извините, вы не можете обратиться в техподдержку");
						}
					}
                    break;

				case "Отзыв":
					if (Check_Patient(msg))
					{
						await client_User.SendTextMessageAsync(msg.Chat.Id, "Поставьте оценку качества обслуживания:", replyMarkup: keyboardFeedback);
					}
					break;

				case "Информация":
					if (Check_Patient(msg))
					{

						try
						{

							if (Check_Patient(msg))
							{
								await client_User.SendTextMessageAsync(msg.Chat.Id, "Что вы хотели бы узнать?", replyMarkup: keyboardInformation);
							}
						}
						catch
						{

						}
					}
					break;

				case "/start":
					if (Check_Patient(msg))
					{
						await client_User.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
						await client_User.SendTextMessageAsync(msg.Chat.Id, msg.Chat.FirstName + ", выберите команду:", replyMarkup: keyboardStart);

					}

					break;

				case "Завершить диалог":

					await client_Chat.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Employee.telegram_id FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "Человек завершил диалог", replyMarkup: new ReplyKeyboardRemove());
					await client_User.SendTextMessageAsync(msg.Chat.Id, "Выберите тему обращения:", replyMarkup: keyboardStart);

					ExecuteNonQuery("UPDATE Appeal SET Appeal.status = 2 WHERE Appeal.id_appeal = (SELECT * FROM(SELECT Appeal.id_appeal FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1) AS t)");

					break;

				default:
					if (Check_Patient(msg))
					{
						var keyboard = new InlineKeyboardMarkup(new[]
											{
											new []
											{
												InlineKeyboardButton.WithCallbackData("Принять", "accept")
											}
										});

						if (!Convert.ToBoolean(ExecuteScalar("SELECT CAST (CASE WHEN EXISTS (SELECT Appeal.id_appeal FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1) THEN 1 ELSE 0 END AS BIT)")).Equals(false))
						{
							SqlConnection connection = new SqlConnection(connStr_users);
							connection.Open();

							string query = "INSERT INTO [Patient message history] (id_patient_message_history, date, text) VALUES (@id_patient_message_history, @date, @text)";
							SqlCommand command_ni = new SqlCommand();
							command_ni.CommandText = query;
							command_ni.Connection = connection;
							command_ni.Parameters.AddWithValue("@id_patient_message_history", Convert.ToInt32(ExecuteScalar("SELECT Appeal.id_appeal FROM Appeal WHERE Appeal.id_patient= (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")));
							command_ni.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
							command_ni.Parameters.AddWithValue("@text", msg.Text);
							command_ni.ExecuteNonQuery();

							connection.Close();

							if (msg.Text == "" || msg.Text == null)
							{
								try
								{
									client_Chat.SendStickerAsync(Convert.ToInt64(ExecuteScalar("SELECT Employee.telegram_id FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), msg.Sticker.FileId);
								}
								catch
								{
									client_User.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
									client_User.SendTextMessageAsync(msg.Chat.Id, "Вы можете отправлять только текстовые сообщения, эмодзи и стикеры.");
								}
							}
							else
							{

								client_Chat.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT Employee.telegram_id FROM Appeal JOIN Employee ON Employee.id_employee = Appeal.id_employee WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND Appeal.status = 1")), "<b>" + msg.Chat.FirstName + "</b>" + "\n" + msg.Text, ParseMode.Html);
							}
						}
						else
						{

							if (!Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_appeal FROM Appeal WHERE id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND status = -2)THEN 1 ELSE 0 END AS BIT)")).Equals(false))
							{
								string[] check_fio = new string[3];

								SqlConnection connection = new SqlConnection(connStr_users);
								connection.Open();
								SqlCommand command = new SqlCommand("SELECT surname, name, patronymic FROM Patient WHERE telegram_id = " + msg.Chat.Id + "", connection);
								SqlDataReader reader = command.ExecuteReader();
								while (reader.Read())
								{
									for (int i = 0; i < 3; i++)
									{
										check_fio[i] = reader[i].ToString();
									}

								}
								connection.Close();

								if (check_fio[0] == "" && check_fio[1] == "" && check_fio[2] == "")
								{
									string[] j = new string[3];
									j = msg.Text.Split(' ');
									ExecuteNonQuery("UPDATE Patient SET surname = '" + j[0] + "', name = '" + j[1] + "', patronymic = '" + j[2] + "'  WHERE telegram_id =  " + msg.Chat.Id + "");
									ExecuteNonQuery("UPDATE Appeal SET status = -1 WHERE id_appeal = (SELECT * FROM (SELECT id_appeal FROM Appeal WHERE id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") AND status = -2) AS T)");

									client_User.SendTextMessageAsync(msg.From.Id, "Опишите свою проблему для формирования заявки (одним сообщением)", replyMarkup: new ReplyKeyboardRemove());

								}
								else
								{
									client_User.SendTextMessageAsync(msg.From.Id, "Неверный формат ФИО, попробуйте ещё раз");
								}

							}
							else
							{
								if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_appeal FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE telegram_id = " + msg.Chat.Id + " AND status = -1)THEN 1 ELSE 0 END AS BIT)")).Equals(false))
								{
									client_User.DeleteMessageAsync(chatId: msg.Chat.Id, msg.MessageId);
								}
								else
								{
									string[] fio = new string[3];

									SqlConnection connection = new SqlConnection(connStr_users);
									connection.Open();
									SqlCommand command = new SqlCommand("SELECT surname, name, patronymic FROM Patient WHERE telegram_id = " + msg.Chat.Id + "", connection);
									SqlDataReader reader = command.ExecuteReader();
									while (reader.Read())
									{
										for (int i = 0; i < 3; i++)
										{
											fio[i] = reader[i].ToString();
										}

									}
									connection.Close();


									await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT telegram_id FROM [Group] WHERE id_group IS NOT NULL")), "#" + Convert.ToInt32(ExecuteScalar("SELECT id_appeal FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE telegram_id = " + msg.Chat.Id + " AND status = -1")) + "\nФИО: " + fio[0] + " " + fio[1] + " " + fio[2] + "\n" + "Тема: Качество обслуживания" + "\n" + "Описание: " + msg.Text, replyMarkup: keyboard);
									ExecuteNonQuery("UPDATE Appeal SET status = 0, description = '" + msg.Text + "' WHERE id_appeal = " + Convert.ToInt32(ExecuteScalar("SELECT id_appeal FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE telegram_id = " + msg.Chat.Id + " AND status = -1")) + "");
									client_User.SendTextMessageAsync(msg.From.Id, msg.From.FirstName + ", ожидайте, в ближайшее время с вами свяжется наш сотрудник.", replyMarkup: new ReplyKeyboardRemove());
								}
							}
						}
					}
					break;
			}
		}

		private async static void TopicAppeal(int topic, long id, int messageid)
		{
			
				string[] check_fio = new string[3];

				SqlConnection connection = new SqlConnection(connStr_users);
				connection.Open();
				SqlCommand command = new SqlCommand("SELECT surname, name, patronymic FROM Patient WHERE telegram_id = " + id + "", connection);
				SqlDataReader reader = command.ExecuteReader();
				while (reader.Read())
				{
					for (int i = 0; i < 3; i++)
					{
						check_fio[i] = reader[i].ToString();
					}

				}
				connection.Close();


				if (check_fio[0] != "" && check_fio[1] != "" && check_fio[2] != "")
				{
					if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS (SELECT Appeal.id_appeal FROM Appeal JOIN Patient ON Appeal.id_patient = Patient.id_patient WHERE Appeal.id_patient = (SELECT id_patient FROM Patient WHERE telegram_id = " + id + ") AND status = 0 OR status = -1 )THEN 1 ELSE 0 END AS BIT)")).Equals(false))
					{
						await client_User.SendTextMessageAsync(id, "Опишите свою проблему для формирования заявки (одним сообщением)", replyMarkup: new ReplyKeyboardRemove());
						
						
						ExecuteNonQuery("INSERT INTO Appeal (id_patient, id_subject_appeal, status) VALUES ((SELECT id_patient FROM Patient WHERE telegram_id = " + id + "),'" + topic + "', -1)");
					}
                    else
                    {
						await client_User.SendTextMessageAsync(id, "Вы уже оставили заявку в техподдержку, дождитесь ответа сотрудника.", replyMarkup: new ReplyKeyboardRemove());
					}


				}
				else
				{
					ExecuteNonQuery("INSERT INTO Appeal (id_patient, id_subject_appeal, status) VALUES ((SELECT id_patient FROM Patient WHERE telegram_id = " + id + ")," + topic + ", -2)");
					await client_User.SendTextMessageAsync(id, "Укажите ваше ФИО (в формате \"Иванов Иван Иванович\")");
				}
				await client_User.DeleteMessageAsync(id, messageid);



		}

		public static void ExecuteNonQuery(string command_sql)
		{
			SqlConnection connection = new SqlConnection(connStr_users);
			connection.Open();
			SqlCommand command = new SqlCommand(command_sql, connection);
			command.ExecuteNonQuery();
			connection.Close();
		}

		public static string ExecuteScalar(string command_sql)
		{
			SqlConnection connection = new SqlConnection(connStr_users);
			connection.Open();
			SqlCommand command = new SqlCommand(command_sql, connection);
			return Convert.ToString(command.ExecuteScalar());
		}

		private static string GetInfo(string sysname)
		{
			string text = "Извините, информация не определена...";

			SqlConnection connection = new SqlConnection(connStr_users);
			connection.Open();
			SqlCommand command = new SqlCommand("SELECT text FROM Information WHERE name = '" + sysname + "'", connection);


			if (Convert.ToString(command.ExecuteScalar()) != null && Convert.ToString(command.ExecuteScalar()) != "")
			{
				text = Convert.ToString(command.ExecuteScalar());
			}
			return text.Replace('~', '\n');
		}

		public static string GetHash(string input)
		{
			var sha1 = SHA1.Create();
			var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
			return Convert.ToBase64String(hash);
		}


		public static bool Check_Patient(Telegram.Bot.Types.Message msg)
        {
			if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_patient FROM Patient WHERE telegram_id = " + msg.Chat.Id + ") THEN 1 ELSE 0 END AS BIT)")).Equals(false))
			{
				if (Convert.ToBoolean(ExecuteScalar("SELECT CAST(CASE WHEN EXISTS(SELECT id_employee FROM Employee WHERE telegram_id = " + msg.Chat.Id + ") THEN 1 ELSE 0 END AS BIT)")).Equals(false)) {

					SqlConnection connection = new SqlConnection(connStr_users);
					connection.Open();

					string query = "INSERT INTO Patient (telegram_id, telegram_username, created, telegram_first_name, telegram_second_name, blocked) VALUES (@telegram_id, @telegram_username, @created, @telegram_first_name, @telegram_second_name, @blocked)";
					SqlCommand command_ni = new SqlCommand();
					command_ni.CommandText = query;
					command_ni.Connection = connection;
					command_ni.Parameters.AddWithValue("@telegram_id", msg.Chat.Id);
					command_ni.Parameters.AddWithValue("@telegram_username", msg.Chat.Username ?? (object)DBNull.Value);
					command_ni.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
					command_ni.Parameters.AddWithValue("@telegram_first_name", msg.Chat.FirstName ?? (object)DBNull.Value);
					command_ni.Parameters.AddWithValue("@telegram_second_name", msg.Chat.LastName ?? (object)DBNull.Value);
					command_ni.Parameters.AddWithValue("@blocked", 0);
					command_ni.ExecuteNonQuery();

					connection.Close();

					return true;
                }
                else
                {
					client_User.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, "+ msg.Chat.FirstName + "!\nЭтот бот предназначен только для пациентов.", replyMarkup: new ReplyKeyboardRemove());
					return false;
				}
			}
            else
            {
				return true;
			}
		}

	}
}

