using MassTransit;
using Models.Hotels;
using Models.Hotels.Dto;
using Models.Transport;
using Models.Transport.Dto;
using Newtonsoft.Json;
using System.Text;
using System;

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

List<HotelFromJson> hotelsFromJson;
using (var r = new StreamReader(@"Init/hotels.json"))
{
    string json = r.ReadToEnd();
    hotelsFromJson = JsonConvert.DeserializeObject<List<HotelFromJson>>(json);
}

int transportIdRange = 10;
int hotelsIdRange = hotelsFromJson.Count();

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
    await Task.Delay(3 * 60000);
    //await Task.Delay(15000);
}

busControl.Stop();

return 0;

async void GenerateHotels(Random rand, int idRange, IBusControl bus)
{
    var str_build = new StringBuilder();
    var name_length = rand.Next(6) + 5;
    for (int i = 0; i < name_length; i++)
    {
        str_build.Append(Convert.ToChar(rand.Next(26) + (int)'A'));
    }
    var hotelName = str_build.ToString();
    var country = hotelsFromJson[rand.Next(hotelsIdRange)].Country;

    var priceForNightForPerson = Math.Round(rand.NextDouble() * (100.0 - 35.0) + 35.0, 2);
    double breakfastPrice;
    if (rand.Next(3) == 0)
    {
        breakfastPrice = -1.0;
    }
    else
    {
        breakfastPrice = Math.Round(rand.NextDouble() * (7.0 - 1.0) + 1.0, 2);
    }
    bool hasWifi;
    if (rand.Next(3) == 0)
    {
        hasWifi = false;
    }
    else
    {
        hasWifi = true;
    }
    int appartments_number = rand.Next(7);
    int casual_rooms_number = rand.Next(6) + 1;
    var res = rand.Next(1000);
    
    if (res < 50)
    {
        // add hotels
        var @event = new AddHotelEvent
        {
            Name = hotelName,
            Country = country,
            BreakfastPrice = breakfastPrice,
            HasWifi = hasWifi,
            PriceForNightForPerson = priceForNightForPerson,
            AppartmentsAmount = appartments_number,
            CasualRoomAmount = casual_rooms_number
        };
        await bus.Publish(@event);
    }
    else if (res < 100)
    {
        // add rooms
        var @event = new AddRoomsInHotelEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            AppartmentsAmountToAdd = appartments_number,
            CasualRoomAmountToAdd = casual_rooms_number
        };
        await bus.Publish(@event);
    }
    else if (res < 150)
    {
        // delete hotel
        var @event = new DeleteHotelEvent
        {
            HotelId = rand.Next(hotelsIdRange)
        };
        await bus.Publish(@event);
    }
    else if (res < 200)
    {
        // delete rooms
        var @event = new DeleteRoomsInHotelEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            AppartmentsAmountToDelete = appartments_number,
            CasualRoomAmountToDelete = casual_rooms_number
        };
        await bus.Publish(@event);
    }
    else if (res < 400)
    {
        // change base price
        var @event = new ChangeBasePriceEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            NewPrice = priceForNightForPerson
        };
        await bus.Publish(@event);
    }
    else if (res < 600)
    {
        // change breakfast price
        var @event = new ChangeBreakfastPriceEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            NewPrice = breakfastPrice
        };
        await bus.Publish(@event);
    }
    else if (res < 800)
    {
        // change wifi availability
        var @event = new ChangeWifiAvailabilityEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            Wifi = hasWifi
        };
        await bus.Publish(@event);
    }
    else
    {
        // change hotel name
        var @event = new ChangeNamesEvent
        {
            HotelId = rand.Next(hotelsIdRange),
            NewName = hotelName
        };
        await bus.Publish(@event);
    }

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
                Id = rand.Next(idRange)+1,
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
                Source = direction ? rand.Next(10)+1 : rand.Next(19)+1,
                Destination = direction ? rand.Next(19)+1 : rand.Next(10)+1,
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
                Id = rand.Next(idRange)+1,
                Price = update > 0 ? Math.Round(rand.NextDouble() * 2000 + 300, 2) : -1,
                AvailableSeats = update < 2 ? rand.Next(100) : -1,
            },
        };
        await bus.Publish(@event);
    }
}

class HotelFromJson
{
    public string Name { get; set; }
    public string Country { get; set; }
}