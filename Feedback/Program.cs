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
			MySqlConnection connection = new MySqlConnection(connStr_users);

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
					case "quality":

						TopicAppeal("Качество обслуживания", ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

						break;

					case "info":

						TopicAppeal("Информация о приеме", ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

						break;
					case "time":

						TopicAppeal("Расписание специалистов", ev.CallbackQuery.From.Id, ev.CallbackQuery.Message.MessageId);

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
					

							if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + ev.CallbackQuery.From.Id + "\" AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')")).Equals(0))
							{
							await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE tb_id_group IS NOT NULL")), "Здравствуйте, " + ev.CallbackQuery.From.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_URL_in_group);
							await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Здравствуйте, " + ev.CallbackQuery.From.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
							}
							else
							{
								if (Convert.ToInt32(ExecuteScalar("SELECT ifnull(id_tb_users,0) FROM tb_users WHERE tb_user_id = " + ev.CallbackQuery.From.Id + "")).Equals(0))
								{
									await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE id_tb_group IS NOT NULL")), "Вы ещё не прошли авторизацию. Для этого перейдите по ссылке " + client_Chat_URL, replyMarkup: keyboard_URL_in_group);

								}
								else
								{

									if (Convert.ToInt32(ExecuteScalar("SELECT EXISTS(SELECT id_tb_appeal FROM tb_appeal WHERE id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + ev.CallbackQuery.From.Id + ") AND status = 1)")).Equals(0))
									{
										if (!Convert.ToInt32(ExecuteScalar("SELECT EXISTS(SELECT id_tb_appeal FROM tb_appeal WHERE id_worker = 0 AND status = 0)")).Equals(0))
										{
											Regex regex = new Regex(@"#[0-9]+");
											MatchCollection matches = regex.Matches(ev.CallbackQuery.Message.Text);

											if (matches.Count == 1)
											{
												foreach (Match match in matches)
												{// находим id обращения и обновляем данные
													ExecuteNonQuery("UPDATE tb_appeal SET id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + ev.CallbackQuery.From.Id + "), status = 1 WHERE id_tb_appeal = " + Convert.ToInt32(match.Value.Replace("#", "")) + "");

													await client_Chat.SendTextMessageAsync(ev.CallbackQuery.From.Id, "Можете приступать к работе с человеком по обращению №" + Convert.ToInt32(match.Value.Replace("#", "")), replyMarkup: keyboard_for_workers_in_dialog);
													await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE id_tb_appeal = " + Convert.ToInt32(match.Value.Replace("#", "")) + "")), "Наш сотрудник - " + ev.CallbackQuery.From.FirstName + " поможет решить вашу проблему!", replyMarkup: keyboard_for_users_in_dialog);

													await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE id_tb_group IS NOT NULL")), "Запрос №"+ Convert.ToInt32(match.Value.Replace("#", "")) + " принят сотрудником - " + ev.CallbackQuery.From.FirstName + ".", replyMarkup: keyboard_URL_in_group);
													await client_Group.DeleteMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE id_tb_group IS NOT NULL")), ev.CallbackQuery.Message.MessageId);
												}
											}
										}
                                        else
                                        {
											await client_Group.DeleteMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE id_tb_group IS NOT NULL")), ev.CallbackQuery.Message.MessageId);
										}
									}
									else
									{
										await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE id_tb_group IS NOT NULL")), ev.CallbackQuery.From.FirstName + ", завершите диалог с клиентом, чтобы принять новое обращение.");
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
					if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\" AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')")) == 0)
					{
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
					}
					else
					{
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nПримите запрос в вашей беседе, чтобы начать диалог с клиентом.");
					}
					break;

				default:
					if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\" AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')")).Equals(0))
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

							
							if (!Convert.ToInt32(ExecuteScalar("SELECT id_tb_users FROM tb_users WHERE login = \"" + GetHash(log_pas[0]) + "\" AND password = \"" + GetHash(log_pas[1]) + "\" AND tb_user_id IS NULL")).Equals(0))
							{
								ExecuteNonQuery("UPDATE tb_users SET tb_user_id = " + msg.Chat.Id + ", tb_user_username_tg = \"" + msg.Chat.Username + "\", created = \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\", first_name = \"" + msg.Chat.FirstName + "\", second_name = \"" + msg.Chat.LastName + "\" WHERE login = \"" + GetHash(log_pas[0]) + "\" AND password = \"" + GetHash(log_pas[1]) + "\"");


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


						if (!Convert.ToInt32(ExecuteScalar("SELECT EXISTS(SELECT id_tb_appeal FROM tb_appeal WHERE id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = 1 ORDER BY id_tb_appeal DESC)")).Equals(0))
						{
							try
							{
								MySqlConnection connection = new MySqlConnection(connStr_users);
								connection.Open();

								string query = "INSERT INTO tb_messages (id_tb_appeal, id_tb_users, text, created) VALUES (@id_tb_appeal, @id_tb, @text, @time)";
								MySqlCommand command = new MySqlCommand();
								command.CommandText = query;
								command.Connection = connection;
								command.Parameters.AddWithValue("@id_tb_appeal", Convert.ToInt32(ExecuteScalar("SELECT id_tb_appeal FROM tb_appeal WHERE id_tb_users = (SELECT id_tb_users FROM tb_appeal WHERE id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = 1) AND id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = 1")));
								command.Parameters.AddWithValue("@id_tb", Convert.ToInt64(ExecuteScalar("SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + "")));
								command.Parameters.AddWithValue("@text", msg.Text);
								command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
									await client_User.SendStickerAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_user_id FROM tb_users WHERE id_tb_users = (SELECT id_tb_users FROM tb_appeal WHERE id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = 1)")), msg.Sticker.FileId);
								}
								catch
								{
									await client_Chat.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
									await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Вы можете отправлять только текстовые сообщения, эмодзи и стикеры.");
								}
							}
							else
							{
								await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_user_id FROM tb_users WHERE id_tb_users = (SELECT id_tb_users FROM tb_appeal WHERE id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = 1)")), "<b>" + msg.Chat.FirstName + "</b>" + "\n" + msg.Text, ParseMode.Html);
							}
						}
						else
						{
							await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Примите запрос в вашей беседе, чтобы начать диалог с клиентом.");
						}
					}

					break;
				case "Завершить диалог":
					if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\" AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')")).Equals(1))
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

						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_tb_users WHERE tb_appeal.id_worker= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "Сотрудник завершил диалог");
						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_tb_users WHERE tb_appeal.id_worker= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "Выберите команду:", replyMarkup: keyboardUser);
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Диалог завершен", replyMarkup: new ReplyKeyboardRemove());

						ExecuteNonQuery("UPDATE tb_appeal SET tb_appeal.status = 2 WHERE tb_appeal.id_tb_appeal = (SELECT * FROM(SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_worker = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1) AS t)");
					}
                    else
                    {
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Здравствуйте, " + msg.Chat.FirstName + "!\nВы не зарегистрированный пользователь. Нажмите на кнопку для регистрации.", replyMarkup: keyboard_registration_for_workers);
					}
					break;
				case "Заблокировать человека":
					if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\" AND login IS NOT NULL AND password IS NOT NULL AND login != '' AND password != '')")).Equals(1))
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

						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_tb_users WHERE tb_appeal.id_worker= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "Сотрудник завершил диалог");
						await client_User.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_tb_users WHERE tb_appeal.id_worker= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "Выберите команду:", replyMarkup: keyboardUs);
						await client_Chat.SendTextMessageAsync(msg.Chat.Id, "Диалог завершен", replyMarkup: new ReplyKeyboardRemove());
						ExecuteNonQuery("UPDATE tb_appeal, tb_users SET tb_appeal.status = 3, tb_users.blocked = 1 WHERE tb_appeal.id_tb_appeal = (SELECT * FROM (SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_worker = (SELECT * FROM(SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AS t1) AND tb_appeal.status = 1) AS t) AND tb_users.id_tb_users = tb_appeal.id_tb_users");
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
				MySqlConnection connection = new MySqlConnection(connStr_users);	
					connection.Open();
					MySqlCommand command = new MySqlCommand("SELECT tb_appeal.id_tb_appeal ,tb_users.fio, tb_appeal.description FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE status = 0", connection);
					MySqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						client_Group.SendTextMessageAsync(msg.Chat.Id, "#" + Convert.ToInt32(reader[0]) + "\nФИО: " + reader[1].ToString() + "\n" + "Тема: Качество обслуживания" + "\n" + "Описание: " + reader[2].ToString(), replyMarkup: keyboard);
					}
					connection.Close();


					if (Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM(SELECT tb_appeal.id_tb_appeal, tb_users.fio, tb_appeal.description FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE status = 0) AS T")).Equals(0))
					{
						client_Group.SendTextMessageAsync(msg.Chat.Id, "Необработанных обращений нет");
					}
			 }
		}

		//иван лох

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
									InlineKeyboardButton.WithCallbackData("Качество обслуживания", "quality")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("Информация о приеме", "info")
								},
								new []
								{
									InlineKeyboardButton.WithCallbackData("Расписание специалистов", "time")
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
					if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}
					if (Convert.ToInt32(ExecuteScalar("SELECT blocked FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + "")).Equals(0))
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
					break;

				case "Отзыв":
					if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}
					if (Convert.ToInt64(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}
					await client_User.SendTextMessageAsync(msg.Chat.Id, "Поставьте оценку качества обслуживания:", replyMarkup: keyboardFeedback);
					break;

				case "Информация":
					if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}

					try
					{


						if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
						{
							ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");

						}
						await client_User.SendTextMessageAsync(msg.Chat.Id, "Что вы хотели бы узнать?", replyMarkup: keyboardInformation);
					}
					catch
					{

					}
					break;

				case "/start":
					await client_User.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
					await client_User.SendTextMessageAsync(msg.Chat.Id, msg.Chat.FirstName + ", выберите команду:", replyMarkup: keyboardStart);


					if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}

					break;

				case "Завершить диалог":

					await client_Chat.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "Человек завершил диалог", replyMarkup: new ReplyKeyboardRemove());
					await client_User.SendTextMessageAsync(msg.Chat.Id, "Выберите тему обращения:", replyMarkup: keyboardStart);

					ExecuteNonQuery("UPDATE tb_appeal SET tb_appeal.status = 2 WHERE tb_appeal.id_tb_appeal = (SELECT * FROM(SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1 ORDER BY id_tb_appeal DESC) AS t)");

					break;

				default:
					if (Convert.ToInt16(ExecuteScalar("SELECT EXISTS(SELECT tb_user_id FROM tb_users WHERE tb_user_id = \"" + msg.Chat.Id + "\")")).Equals(0))
					{
						ExecuteNonQuery("INSERT INTO tb_users (tb_user_id, tb_user_username_tg, created, first_name, second_name) VALUES (\"" + msg.Chat.Id + "\",\"" + msg.Chat.Username + "\",\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + msg.Chat.FirstName + "\",\"" + msg.Chat.LastName + "\")");
					}
					var keyboard = new InlineKeyboardMarkup(new[]
											{
											new []
											{
												InlineKeyboardButton.WithCallbackData("Принять", "accept")
											}
										});

					if (!Convert.ToInt32(ExecuteScalar("SELECT EXISTS (SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1 ORDER BY id_tb_appeal DESC) AS T")).Equals(0))
					{
						MySqlConnection connection = new MySqlConnection(connStr_users);
						connection.Open();

						string query = "INSERT INTO tb_messages (id_tb_appeal, id_tb_users, text, created) VALUES (@id_tb_appeal, @id_tb, @text, @time)";
						MySqlCommand command_ni = new MySqlCommand();
						command_ni.CommandText = query;
						command_ni.Connection = connection;
						command_ni.Parameters.AddWithValue("@id_tb_appeal", Convert.ToInt32(ExecuteScalar("SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1 ORDER BY id_tb_appeal DESC")));
						command_ni.Parameters.AddWithValue("@id_tb", Convert.ToInt32(ExecuteScalar("SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + "")));
						command_ni.Parameters.AddWithValue("@text", msg.Text);
						command_ni.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
						command_ni.ExecuteNonQuery();

						connection.Close();

						if (msg.Text == "" || msg.Text == null)
						{
							try
							{
								client_Chat.SendStickerAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), msg.Sticker.FileId);
							}
							catch
							{
								client_User.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
								client_User.SendTextMessageAsync(msg.Chat.Id, "Вы можете отправлять только текстовые сообщения, эмодзи и стикеры.");
							}
						}
						else
						{

							client_Chat.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_users.tb_user_id FROM tb_appeal JOIN tb_users ON tb_users.id_tb_users = tb_appeal.id_worker WHERE tb_appeal.id_tb_users= (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND tb_appeal.status = 1")), "<b>" + msg.Chat.FirstName + "</b>" + "\n" + msg.Text, ParseMode.Html);
						}
					}
					else
					{

						if (!Convert.ToInt32(ExecuteScalar("SELECT EXISTS(SELECT id_tb_appeal FROM tb_appeal WHERE id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = -2 ORDER BY id_tb_appeal DESC)")).Equals(0))
						{
							if (ExecuteScalar("SELECT fio FROM tb_users WHERE id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ")").Equals(null) || ExecuteScalar("SELECT fio FROM tb_users WHERE id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ")").Equals(""))
							{
								string[] j = new string[3];
								j = msg.Text.Split(' ');
								if (j[0] != "" && j[1] != "" && j[2] != "")
								{

									ExecuteNonQuery("UPDATE tb_users SET fio = '" + msg.Text + "' WHERE id_tb_users = (SELECT * FROM (SELECT id_tb_users FROM tb_users WHERE tb_user_id =  " + msg.Chat.Id + ") AS T)");
									ExecuteNonQuery("UPDATE tb_appeal SET status = -1 WHERE id_tb_appeal = (SELECT * FROM (SELECT id_tb_appeal FROM tb_appeal WHERE id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + ") AND status = -2 ORDER BY id_tb_appeal DESC) AS T)");

									client_User.SendTextMessageAsync(msg.From.Id, "Опишите свою проблему для формирования заявки (одним сообщением)", replyMarkup: new ReplyKeyboardRemove());
								}
								else
								{
									client_User.SendTextMessageAsync(msg.From.Id, "Неверный формат ФИО, попробуйте ещё раз");
								}

							}

						}
						else
						{
							if (Convert.ToInt32(ExecuteScalar("SELECT EXISTS(SELECT id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE tb_user_id = " + msg.Chat.Id + " AND status = -1 ORDER BY id_tb_appeal DESC)")).Equals(0))
							{
								client_User.DeleteMessageAsync(chatId: msg.Chat.Id, msg.MessageId);
							}
							else
							{
								await client_Group.SendTextMessageAsync(Convert.ToInt64(ExecuteScalar("SELECT tb_id_group FROM tb_group WHERE tb_id_group IS NOT NULL")), "#" + Convert.ToInt32(ExecuteScalar("SELECT id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE tb_user_id = " + msg.Chat.Id + " AND status = -1 ORDER BY id_tb_appeal DESC")) + "\nФИО: " + Convert.ToString(ExecuteScalar("SELECT fio FROM tb_users WHERE tb_user_id = " + msg.Chat.Id + "")) + "\n" + "Тема: Качество обслуживания" + "\n" + "Описание: " + msg.Text, replyMarkup: keyboard);
								ExecuteNonQuery("UPDATE tb_appeal SET status = 0, description = \"" + msg.Text + "\" WHERE id_tb_appeal = " + Convert.ToInt32(ExecuteScalar("SELECT id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE tb_user_id = " + msg.Chat.Id + " AND status = -1 ORDER BY id_tb_appeal DESC")) + "");
								client_User.SendTextMessageAsync(msg.From.Id, msg.From.FirstName + ", ожидайте, в ближайшее время с вами свяжется наш сотрудник.", replyMarkup: new ReplyKeyboardRemove());
							}
						}
					}
					break;
			}
		}

		private async static void TopicAppeal(string topic, long id, int messageid)
		{
			try
			{
				if (!ExecuteScalar("SELECT fio FROM tb_users WHERE tb_user_id = " + id + "").Equals(null) && !ExecuteScalar("SELECT fio FROM tb_users WHERE tb_user_id = " + id + "").Equals(""))
				{
					if (Convert.ToInt32(ExecuteScalar("SELECT EXISTS (SELECT tb_appeal.id_tb_appeal FROM tb_appeal JOIN tb_users ON tb_appeal.id_tb_users = tb_users.id_tb_users WHERE tb_appeal.id_tb_users = (SELECT id_tb_users FROM tb_users WHERE tb_user_id = "+id+ ") AND status = 0 OR status = -1 )")).Equals(0))
					{
						await client_User.SendTextMessageAsync(id, "Опишите свою проблему для формирования заявки (одним сообщением)", replyMarkup: new ReplyKeyboardRemove());
						ExecuteNonQuery("INSERT INTO tb_appeal (id_tb_users, topic, status) VALUES ((SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + id + "),'" + topic + "', -1)");
					}
                    else
                    {
						await client_User.SendTextMessageAsync(id, "Вы уже оставили заявку в техподдержку, дождитесь ответа сотрудника.", replyMarkup: new ReplyKeyboardRemove());
					}


				}
				else
				{
					ExecuteNonQuery("INSERT INTO tb_appeal (id_tb_users, topic, status) VALUES ((SELECT id_tb_users FROM tb_users WHERE tb_user_id = " + id + "),'" + topic + "', -2)");
					await client_User.SendTextMessageAsync(id, "Укажите ваше ФИО (в формате \"Иванов Иван Иванович\")");
				}
				await client_User.DeleteMessageAsync(id, messageid);
			}
			catch
			{

			}


		}

		public static void ExecuteNonQuery(string command_sql)
		{
			MySqlConnection connection = new MySqlConnection(connStr_users);
			connection.Open();
			MySqlCommand command = new MySqlCommand(command_sql, connection);
			command.ExecuteNonQuery();
			connection.Close();
		}

		public static string ExecuteScalar(string command_sql)
		{
			MySqlConnection connection = new MySqlConnection(connStr_users);
			connection.Open();
			MySqlCommand command = new MySqlCommand(command_sql, connection);
			return Convert.ToString(command.ExecuteScalar());
		}

		private static string GetInfo(string sysname)
		{
			string text = "Извините, информация не определена...";

			MySqlConnection connection = new MySqlConnection(connStr_users);
			connection.Open();
			MySqlCommand command = new MySqlCommand("SELECT text FROM tb_info WHERE sysname = '" + sysname + "'", connection);


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

	}
}

