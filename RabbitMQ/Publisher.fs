﻿namespace Alex75.Utils.RabbitMQ

open System
open System.Text
open RabbitMQ.Client
open common


type Publisher(config:Config) =    
    
    let factory = lazy(
        let factory = new ConnectionFactory(Uri=Uri(config.Url))
        factory.Uri <- Uri(config.Url)
        factory
    )

    let connection = factory.Value.CreateConnection()
    let channel = connection.CreateModel()


    member this.CreateQueue(queue:string) =
        let result = channel.QueueDeclare(queue=queue, durable=false, exclusive=false, autoDelete = false, arguments = null)
        ()

    // publish to exchange or to queue ???
    member this.Send(message:string, exchange:string, routingKey:string) = 
        let factory = new ConnectionFactory()
        factory.Uri <- Uri(config.Url)

        let body = ReadOnlyMemory(Encoding.UTF8.GetBytes(message))

        //channel.BasicPublish(exchange="", routingKey=routingKey, basicProperties=null, body=body)
        channel.BasicPublish(exchange=exchange, routingKey=routingKey, basicProperties=null, body=body)

    
    interface IDisposable with
        member this.Dispose() =
            channel.Dispose()
            connection.Dispose()