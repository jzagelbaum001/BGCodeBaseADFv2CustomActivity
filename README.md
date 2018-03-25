# Microsoft Document Translator refactor for ADFv2 Custom Activity Sample
This project is a refactor of the Microsoft Document Translator: https://github.com/MicrosoftTranslator/DocumentTranslator The Microsoft Document Translator translates Microsoft Office, plain text, HTML, PDF files and SRT caption files, from and to any of the 60+ languages supported by the Microsoft Translator web service.
Document Translator uses the customer's own credentials and subscription to perform the Translation. Document Translator can also use custom MT systems trained via the Microsoft Translator Hub (https://microsoft.com/translator/hub.aspx).

#For Demonstration Only
This solution is presented for demonstration purposes only. The code base includes an arm template for creating a data factory pipeline and linked services appropriate to run the activity. 

#Known Limitations
-This version is limited to running text files, but can easily be extended to include functionality of different document types already handled by the translator. 
-The translation services key is stored as an extended property in the activty definition. ADFv2 does not currently support key vault management when using custom activities, however, additional functionality could be added to call from key vault instead of extended properties. The key value itself is TLS encrypted when sent to cognitive services. Likewise, Azure security handles access to authoring data factory activities.

 