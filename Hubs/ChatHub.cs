using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Consumers;

namespace RabbitMQ.Hubs;

// Specifies methods available for clients
public interface IChatClientNotifications
{
	public Task RecieveMessage(string message);
	public Task RecieveNotification(string message);
	public Task SelfPing();
}

public interface IChatClient
{
	public Task SendMessage(string message);
	public Task SendPing();
	public Person GetData();
}

// Hub<T> specifies methods available to call for clients
//[Authorize]
public class ChatHub : Hub<IChatClientNotifications>, IChatClient
{
	// Invoked by clients
	public async Task SendMessage(string message)
	{
		Console.WriteLine("Recieved message: " + message);

		// Invoked for clients
		await Clients.Others.RecieveMessage(message);
	}

	public Person GetData()
	{
		return new Person() { Age = 777, Name = "Fikus" };
	}

	public async Task SendPing()
	{
		await Clients.Caller.SelfPing();
	}

	// When someone (client) connects to the hub
	public override async Task OnConnectedAsync()
	{
		await Clients.All.RecieveNotification($"Someone connected with Id: {Context.ConnectionId}");
		await base.OnConnectedAsync();
	}

	// When someone (client) disconnected from the hub
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		await Clients.Others.RecieveNotification($"Someone disconnected with Id: {Context.ConnectionId}");
		await base.OnDisconnectedAsync(exception);
	}
}


