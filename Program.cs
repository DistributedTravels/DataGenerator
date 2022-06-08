using MassTransit;
using Models.Hotels;
using Models.Hotels.Dto;
using Models.Transport;
using Models.Transport.Dto;

var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host("rabbitmq", "/", h =>
    {
        h.Username("guest");
        h.Password("guest");
    });
});
busControl.Start();


var rand = new Random();

int transportIdRange = 10;
int hotelsIdRange = 90;

while (true) // never ending :)
{
    if (rand.Next(2) == 0)
        GenerateHotels(rand, hotelsIdRange, busControl);
    else
    {
        GenerateTransport(rand, transportIdRange, busControl);
        if (transportIdRange <= 6000)
            transportIdRange += 1;
    }
    await Task.Delay(3*60000);
    //await Task.Delay(15000);
}

busControl.Stop();

return 0;

void GenerateHotels(Random rand, int idRange, IBusControl bus)
{

}

async void GenerateTransport(Random rand, int idRange, IBusControl bus)
{
    var res = rand.Next(1000);
    if (res < 30)
    {
        // delete
        var @event = new UpdateTransportTOEvent()
        {
            Action = UpdateTransportTOEvent.Actions.DELETE,
            Table = UpdateTransportTOEvent.Tables.TRAVEL,
            TravelDetails = new TravelChangeDto()
            {
                Id = rand.Next(idRange),
            },
        };
        await bus.Publish(@event);
    }
    else if (res > 969)
    {
        // new
        var direction = rand.Next(2) == 0 ? false : true;
        var @event = new UpdateTransportTOEvent()
        {
            Action = UpdateTransportTOEvent.Actions.NEW,
            Table = UpdateTransportTOEvent.Tables.TRAVEL,
            TravelDetails = new TravelChangeDto()
            {
                Source = direction ? rand.Next(10) : rand.Next(19),
                Destination = direction ? rand.Next(19) : rand.Next(10),
                Direction = direction,
                DepartureTime = DateTime.Now.AddMonths(rand.Next(2) + 1).AddDays(rand.Next(20)),
                AvailableSeats = rand.Next(100),
                Price = Math.Round(rand.NextDouble() * 2000 + 300, 2),
            },
        };
        await bus.Publish(@event);
    }
    else
    {
        var update = rand.Next(3);
        // update flight
        var @event = new UpdateTransportTOEvent()
        {
            Action = UpdateTransportTOEvent.Actions.UPDATE,
            Table = UpdateTransportTOEvent.Tables.TRAVEL,
            TravelDetails = new TravelChangeDto()
            {
                Id = rand.Next(idRange),
                Price = update > 0 ? Math.Round(rand.NextDouble() * 2000 + 300, 2) : -1,
                AvailableSeats = update < 2 ? rand.Next(100) : -1,
            },
        };
        await bus.Publish(@event);
    }
}