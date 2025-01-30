# Próximo Manual 

This project consists of a few functions that serve as the back end for the site manual.proximoturno.com.br. 
The main purpose of this project is to help board game players with any questions about selected games.

## Technologies

The following technologies were used to create this project:
- Azure functions
- Azure Cosmos DB
- Gemini API  
    Model: gemini-1.5-flash-002

## Required Environments
- GOOGLE_API_KEY  
    Key used to access the Gemini API
- COSMO_ENDPOINT_URI
- COSMO_PRIMARY_KEY

## How to uplod new versions

After finished development run  
>func azure functionapp publish ManualDoJogo

