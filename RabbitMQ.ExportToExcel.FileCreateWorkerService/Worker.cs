using ClosedXML.Excel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.ExportToExcel.Common.Models;
using RabbitMQ.ExportToExcel.FileCreateWorkerService.Models;
using RabbitMQ.ExportToExcel.FileCreateWorkerService.Services;
using System.Data;
using System.Text;
using System.Text.Json;

namespace RabbitMQ.ExportToExcel.FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMQClientService _rabbitMQClientService;
        private IModel _channel;


        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, RabbitMQClientService rabbitMQClientService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitMQClientService = rabbitMQClientService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitMQClientService.Connect();
            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AsyncEventingBasicConsumer consumer = new(_channel);

            _channel.BasicConsume(
                queue: RabbitMQClientService.QueueName,
                autoAck: false,
                consumer: consumer
                );

            consumer.Received += Consumer_Received;

            return Task.CompletedTask;

        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));

            using MemoryStream memoryStream = new();

            XLWorkbook workBook = new();
            DataSet dataSet = new();
            dataSet.Tables.Add(GetTable("products"));

            workBook.Worksheets.Add(dataSet);
            workBook.SaveAs(memoryStream);

            MultipartFormDataContent multipartFormDataContent = new();
            multipartFormDataContent.Add(
                content: new ByteArrayContent(memoryStream.ToArray()),
                name: "file",
                fileName: Guid.NewGuid().ToString() + ".xlsx");

            var baseUrl = "https://localhost:44358/api/files/";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createExcelMessage.FileId}", multipartFormDataContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"File ( Id : {createExcelMessage.FileId} ) was created by successful");
                    _channel.BasicAck(
                        deliveryTag: @event.DeliveryTag,
                        multiple: false);
                }
            }

        }

        private DataTable GetTable(string tableName)
        {
            List<Models.Product> products;

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();

                products = context.Products.ToList();
            }

            DataTable table = new DataTable
            {
                TableName = tableName
            };

            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ProductNumber", typeof(string));
            table.Columns.Add("Color", typeof(string));

            products.ForEach(x =>
            {
                table.Rows.Add(x.ProductId, x.Name, x.ProductNumber, x.Color);
            });

            return table;

        }
    }
}