using Network;
using Network.Enums;
using System;
using System.Collections.Generic;
using TestServerClientPackets.ExamplePacketsOne;
using TestServerClientPackets.ExamplePacketsOne.Containers;

namespace NetworkTestClient
{
    internal class ObjectExample
    {
        private ClientConnectionContainer container;
        private Random random = new Random();

        public void Demo()
        {
            //1. Establish a connection to the server.
            container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += OnConnectionEstablished;
        }

        private void OnConnectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{type.ToString()} Connection established");
            //3. Register what happens if we receive a packet of type "CalculationResponse"
            container.RegisterPacketHandler<AddStudentToDatabaseResponse>(StudentToDatabaseResult, this);
            //4. Send a calculation request.
            connection.Send(GenerateDataSet(), this);
        }

        private void StudentToDatabaseResult(AddStudentToDatabaseResponse response, Connection connection)
        {
            Console.WriteLine($"Server response {connection.GetType()}> {response.Result.ToString()}");
        }

        private AddStudentToDatabaseRequest GenerateDataSet()
        {
            Student student = new Student
            {
                Birthday = new Date() { Day = 15, Month = 3, Year = 1991 },
                FirstName = "Martin",
                Lastname = "Mayer",
                VisitedPlaces = GenerateVisitedPlaces()
            };

            AddStudentToDatabaseRequest request = new AddStudentToDatabaseRequest(student) { Rooms = GenerateRooms() };
            return request;
        }

        private List<GeoCoordinate> GenerateVisitedPlaces()
        {
            List<GeoCoordinate> geoCoordinates = new List<GeoCoordinate>();
            while (random.Next(0, 101) <= 95 && geoCoordinates.Count < 500)
            {
                GeoCoordinate geoCoordinate = new GeoCoordinate
                {
                    Latitude = (float)random.NextDouble(),
                    Longitude = (float)random.NextDouble()
                };
                geoCoordinates.Add(geoCoordinate);
            }
            return geoCoordinates;
        }

        private List<string> GenerateRooms()
        {
            List<string> rooms = new List<string>();
            while (random.Next(0, 101) <= 95 && rooms.Count < 100)
                rooms.Add(random.Next(0, 501).ToString());
            return rooms;
        }
    }
}