using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

public class GraphChatExample
{
    private static string tenantId = "Your Azure AD TenantID";  // Azure AD Tenant ID
    private static string clientId = "Your Azure AD ClientID";  // Azure AD Application (Client) ID
    private static string clientSecret = "Your Azure AD Client Secret";  // Azure AD Application Client Secret (isteğe bağlı, izinler için)
    private static string redirectUri = "http://localhost"; // Typically, localhost is used for development

    public static async Task Main(string[] args)
    {
        //CREATE USER AND SET LICENCE---------------------------------------
        // Azure Identity: ClientSecretCredential

        var credential3 = new ClientSecretCredential(tenantId, clientId, clientSecret);
        // Microsoft Graph client
        var graphClient3 = new GraphServiceClient(credential3);
        
        // 0. User Listesini alma:
        var users = await GetAllUsersList(clientId, tenantId, clientSecret);

        // Kullanıcıları yazdır
        foreach (var user in users)
        {
            Console.WriteLine($"ID: {user.Id}, Ad: {user.DisplayName}, E-posta: {user.Mail}");
        }

        // 1. Kullanıcı ID'sini e-posta ile bulma
        var email = "keepnetappbot@keepnetlabs.com"; // Kullanıcının e-posta adresi       
        var email2 = "bora.kasmer@keepnetlabs.com"; // Kullanıcının e-posta adresi       
        // Kullanıcı ID'sini bul

        var userId = await FindUserIdByEmail(clientId, tenantId, clientSecret, email);
        var userId2 = await FindUserIdByEmail(clientId, tenantId, clientSecret, email2);

        if (!string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"Kullanıcı({email}) ID'si: {userId}");
        }
        else
        {
            Console.WriteLine("Kullanıcı bulunamadı.");
        }
        if (!string.IsNullOrEmpty(userId2))
        {
            Console.WriteLine($"Kullanıcı({email2}) ID'si: {userId2}");
        }
        else
        {
            Console.WriteLine("Kullanıcı bulunamadı.");
        }

        // 2. One-on-One Chat Oluşturma      
        var senderId = userId;
        var recipientId = userId2;

        // Create the 1:1 chat
        // Azure Identity: ClientSecretCredential
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Microsoft Graph client
        var graphClient2 = new GraphServiceClient(credential);
        var chatId = await CreateOneOnOneChat(graphClient2, senderId, recipientId);

        if (!string.IsNullOrEmpty(chatId))
        {
            Console.WriteLine($"1:1 Chat oluşturuldu. Chat ID: {chatId}");
        }
        else
        {
            Console.WriteLine("1:1 Chat oluşturulamadı.");
        }

        //Workinggg-------------------
        string[] scopes = { "User.Read" };
        UsernamePasswordCredential usernamePasswordCredential = new UsernamePasswordCredential("keepnetappbot@keepnetlabs.com", "********", tenantId, clientId);
        GraphServiceClient graphClient = new GraphServiceClient(usernamePasswordCredential, scopes); // you can pass the TokenCredential directly to the GraphServiceClient

        //-------------------

        // 3. Mesaj Gönderme
        await SendMessageToChat(graphClient, chatId, "<h1>Deepseek'den size atanmis bir egitiminiz var!</h1><p><a href='https://borakasmer.com'>İşlem başarıyla tamamlandı." +            
            "</a></p><img src='https://media4.giphy.com/media/v1.Y2lkPTc5MGI3NjExMHgxbTd1ZzBoem1iamNyZXkyanE2MTVwbWp6NHFyam4zc2lqczc2NCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/cPZZKlacdHtoVrjPzq/giphy.gif' " +
            "alt='Başardık GIF' width='300'><p>Devam edebilirsiniz!</p>");
    }

    //Microsoft Graph kimlik doğrulaması ve istemciyi ayarlama
    private static GraphServiceClient GetAuthenticatedGraphClient()
    {
        var clientSecretCredential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret,
            new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            });

        return new GraphServiceClient(clientSecretCredential);
    }

    private static async Task<Chat> CreateOneOnOneChat_Old(GraphServiceClient graphClient, string userId1, string userId2)
    {
        var chatMembers = new List<ConversationMember>
{
    new AadUserConversationMember
    {
        OdataType = "#microsoft.graph.aadUserConversationMember",
        Roles = new List<string> { "owner" },
        AdditionalData = new Dictionary<string, object>
        {
            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId1}')" }
        }
    },
    new AadUserConversationMember
    {
        OdataType = "#microsoft.graph.aadUserConversationMember",
        Roles = new List<string> { "owner" },
        AdditionalData = new Dictionary<string, object>
        {
            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId2}')" }
        }
    }
};

        var requestBody = new Chat
        {
            ChatType = ChatType.OneOnOne,
            Members = chatMembers
        };

        var chat = await graphClient.Chats.PostAsync(requestBody);
        Console.WriteLine($"Chat created with ID: {chat.Id}");

        return chat;
    }

    public static async Task<List<User>> GetAllUsersList(string clientId, string tenantId, string clientSecret)
    {
        // Azure Identity: ClientSecretCredential
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Microsoft Graph client
        var graphClient = new GraphServiceClient(credential);

        var usersList = new List<User>();

        try
        {
            // Fetch the first page of users
            var users = await graphClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "id" };
                    requestConfiguration.QueryParameters.Top = 100; // Optional: fetch up to 100 users per page
                });

            // Add users to the list
            if (users?.Value != null)
            {
                usersList.AddRange(users.Value);
            }

            // Handle pagination
            while (users?.OdataNextLink != null)
            {
                // Fetch the next page of users
                users = await graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = 100; // Optional
                    });

                if (users?.Value != null)
                {
                    usersList.AddRange(users.Value);
                }
            }
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return usersList;
    }

    public static async Task<string> FindUserIdByEmail(string clientId, string tenantId, string clientSecret, string email)
    {
        // Azure Identity: ClientSecretCredential
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Microsoft Graph client
        var graphClient = new GraphServiceClient(credential);

        try
        {
            // Kullanıcıyı e-posta ile bulma
            var users = await graphClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"mail eq '{email}'";
                    requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail" };
                });

            // Eğer kullanıcı varsa ID'sini döndür
            if (users?.Value != null && users.Value.Count > 0)
            {
                var user = users.Value[0]; // İlk eşleşen kullanıcıyı al
                return user.Id;
            }
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
        }

        // Kullanıcı bulunamazsa null döndür
        return null;
    }

    public static async Task<string> CreateOneOnOneChat(GraphServiceClient graphClient, string senderId, string recipientId)
    {
        try
        {
            // 1:1 Chat oluşturma
            var chat = new Chat
            {
                ChatType = ChatType.OneOnOne,
                Members = new List<ConversationMember>
            {
                new AadUserConversationMember
                {
                    OdataType="#microsoft.graph.aadUserConversationMember",
                    Roles = new List<string> { "owner" },
                    AdditionalData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        //{"@odata.type", "#microsoft.graph.aadUserConversationMember"},
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users/{senderId}"} // OData binding for sender
                    },
                    UserId = senderId
                } as ConversationMember, // Cast to ConversationMember
                new AadUserConversationMember
                {
                    OdataType="#microsoft.graph.aadUserConversationMember",
                    Roles = new List<string> { "owner" },
                    AdditionalData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        //{"@odata.type", "#microsoft.graph.aadUserConversationMember"},
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users/{recipientId}"} // OData binding for recipient
                    },
                    UserId = recipientId
                } as ConversationMember // Cast to ConversationMember
            }
            };

            // Chat oluştur ve ID'sini al
            var createdChat = await graphClient.Chats.PostAsync(chat);

            return createdChat.Id;
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            return null;
        }
    }

    public static async Task SendMessageToChat(GraphServiceClient graphClient, string chatId, string message)
    {
        try
        {
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    //Content = message
                    Content = message
                }
            };

            // Send the message to the chat
            await graphClient.Chats[chatId].Messages.PostAsync(chatMessage);

            Console.WriteLine($"Message sent to chat {chatId}: {message}");
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public static User CreateUser(GraphServiceClient graphClient, string displayName, string mail)
    {
        try
        {
            // Create a new user
            var user = new User
            {
                DisplayName = displayName,
                MailNickname = "Bot",
                Mail = mail,
                UserPrincipalName = mail,
                AccountEnabled = true,
                UserType = UserType.Guest.ToString(),
                ShowInAddressList = false,
                PasswordPolicies = "DisablePasswordExpiration, DisableStrongPassword",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = false,
                    Password = "P@ssw0rd1234",
                },
            };
            // Add the user
            graphClient.Users.PostAsync(user).Wait();
            Console.WriteLine($"User {displayName} created successfully.");
            return user;
        }
        catch (ServiceException ex)
        {
            Console.WriteLine($"Error creating user: {ex.Message}");
            return new User();
        }
    }
}