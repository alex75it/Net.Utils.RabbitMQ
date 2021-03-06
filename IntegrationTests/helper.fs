﻿module helper

open System
open System.Net.Http
open System.Text

open Newtonsoft.Json.Linq
open Alex75.Utils.RabbitMQ
open secrets
open common


let createConsumer() =
    let configuration = {Url=secrets.URL}
    new Consumer(configuration)

type TestObject (aString:string, aNumber:decimal, aDate:DateTime) = 

    member this.AString with get() = aString
    member this.ANumber with get() = aNumber
    member this.ADate with get() = aDate



let basiAuth () = 
    let base64Encode (s:string) =
        let bytes = Encoding.UTF8.GetBytes(s)
        Convert.ToBase64String(bytes)

    sprintf "%s:%s" username password |> base64Encode |> sprintf "Basic %s"


let listQueues () = 

    use client = new HttpClient()
    client.BaseAddress <- Uri(sprintf "%s/%s" baseAddress vhost)
    client.DefaultRequestHeaders.Add("Authorization", basiAuth())
    let response = client.GetAsync("api/queues").Result

    //Assert.That(response.IsSuccessStatusCode)
    if not(response.IsSuccessStatusCode) then failwithf "Failed to load queues. (%O) %s" response.StatusCode response.ReasonPhrase

    let json = response.Content.ReadAsStringAsync().Result

    let parseQueue (json:JToken) = (json.Value<string>("name"), json.Value<int>("messages"))
    let queues = JArray.Parse(json) |> Seq.map parseQueue |> List.ofSeq
    queues


let listExchanges () = 

    use client = new HttpClient()
    client.BaseAddress <- Uri(sprintf "%s/%s" baseAddress vhost)
    client.DefaultRequestHeaders.Add("Authorization", basiAuth())
    let response = client.GetAsync("api/exchanges").Result

    //Assert.That(response.IsSuccessStatusCode)
    if not(response.IsSuccessStatusCode) then failwithf "Failed to load exchanges. (%O) %s" response.StatusCode response.ReasonPhrase

    let json = response.Content.ReadAsStringAsync().Result    

    // easy counter (none = zero)
    let parseMessageStats (json:JToken) = 
        match json.["message_stats"] with
        | null -> 0
        | stats -> stats.Value<int>("publish_in")
        

    let parseExchange (json:JToken) = {
        Name=json.Value<string>("name"); Type=json.Value<string>("type"); VHost= json.Value<string>("vhost")
        MessageStats_PublishIn = parseMessageStats json
    }

    let exchanges = JArray.Parse(json) |> Seq.map parseExchange |> List.ofSeq
    exchanges


let deleteQueue (name, failsIfNotFound) = 

    use client = new HttpClient()
    client.BaseAddress <- Uri(sprintf "%s/%s" baseAddress vhost)
    client.DefaultRequestHeaders.Add("Authorization", basiAuth())
    let response = client.DeleteAsync(sprintf "api/queues/%s/%s" vhost name).Result

    match response.IsSuccessStatusCode with
    | true -> ()
    | false when response.StatusCode = Net.HttpStatusCode.NotFound && not failsIfNotFound -> ()
    | _ -> failwithf "Failed to delete queue \"%s\". (%O) %s" name response.StatusCode response.ReasonPhrase


let deleteExchange (name, failsIfNotFound) = 

    use client = new HttpClient()
    client.BaseAddress <- Uri(sprintf "%s/%s" baseAddress vhost)
    client.DefaultRequestHeaders.Add("Authorization", basiAuth())

    let mutable counter = 1
    while counter < 20 && listExchanges() |> List.exists (fun ex -> ex.Name = name) do

    let response = client.DeleteAsync(sprintf "api/exchanges/%s/%s" vhost name).Result

    match response.IsSuccessStatusCode with
    | true -> ()
    | false when response.StatusCode = Net.HttpStatusCode.NotFound && not failsIfNotFound -> ()
    | _ -> failwithf "Failed to delete exchange \"%s\". (%O) %s" name response.StatusCode response.ReasonPhrase

    Threading.Thread.Sleep(250)


let createVHost (name) = 

    use client = new HttpClient()
    client.BaseAddress <- Uri(sprintf "%s/%s" baseAddress vhost)
    //client.DefaultRequestHeaders.Accept.Add( MediaTypeWithQualityHeaderValue( "", "")
    client.DefaultRequestHeaders.Add("Accept", "application/json")
    client.DefaultRequestHeaders.Add("Authorization", basiAuth())

    let response = client.PutAsync(sprintf "api/vhost/%s/%s" vhost name, null).Result

    match response.IsSuccessStatusCode with
    | true -> ()
    //| false when response.StatusCode = Net.HttpStatusCode.NotFound && not failsIfNotFound -> ()
    | _ -> failwithf "Failed to create vhost \"%s\". (%O) %s" name response.StatusCode response.ReasonPhrase