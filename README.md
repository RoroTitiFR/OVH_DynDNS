# OVH DynDNS

Fully customizable DynDNS, on the cheap!

**LICENCE: WTFPL** ("no really...")

## Features

- Supports multiple sub-domains and top level domains
- Supports email notifications on domain updates
- Works on Windows, Mac and Linux
- **Cheap: obtain a fully customizable DDNS for 5€/year or less. Equivalent fully managed services cost around 40€/month!** 
- **Open-source!**

## Limitations

- Only supports European OVH endpoint for now.

## How to use (Linux) ?

0. Generate OVH API keys by creating an application here : [Creating an API Application](https://eu.api.ovh.com/createApp/). 

1. Download and extract the latest release.
   ```
   unzip file.zip
   ```

2. Copy the ``OVH_DynDNS_v2/config.json`` of this repository into the previously extracted release directory.

3. Edit the values of the ``config.json`` file to suit your needs. For now, the only required values are **OvhApplicationKey**, **OvhApplicationSecret** and **OvhDomainName**.
You can set the other properties later.

4. Request your Consumer Key by executing the script with get-ck parameter.
   ```
   ./OVH_DynDNS_v2 get-ck
   ```
   Once you obtain your consumer key, set the **OvhConsumerKey** property of the ``config.json`` file.

5. Run the DDNS agent. It will check for domain updates every 5 minutes.
   ```
   ./OVH_DynDNS_v2
   ```
## ``config.json`` explanations

|Property|Usage|
|---|---|
|OvhApplicationKey|Your OVH application key|
|OvhApplicationSecret|Your OVH application secret|
|OvhConsumerKey|Your OVH consumer key. See in guide how to get it.|
|OvhDomainName|Your OVH domain that you want to transform into dynamic domain|
|EnableEmailNotifications|Enable or not domain updates email notifications|
|MailSmtpHost|Your SMTP host|
|MailSmtpPort|Your SMTP host port|
|MailSmtpUsername|Your SMTP account username|
|MailSmtpPassword|Your SMTP account password|
|MailEnableSsl|Enable is your SMTP server uses SSL encryption|
|MailFrom|The emitter of the notification emails. **May be overwritten by certain SMTP server like Gmail!**|
|MailTo|The receiver of the notifications emails|
   
## Run as a SystemD service

Coming soon...

## Compiling the source code

The code is written in C#, running with .NET Core 3.1 runtime

Coming soon...

## Future improvements

- Sub-domain inclusion/exclusion (white-list/black-list modes)
- Multiple domains support in one agent only
- Multiple email notification targets