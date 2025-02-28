import pino from "pino";
import pinoMultiStream from 'pino-multi-stream';
import datadog from 'pino-datadog';

export class Logger {
    private static _logger: pino.Logger;

    static {
        let streamToDataDog = datadog.createWriteStreamSync({
            eu: false,
            apiKey: process.env.DD_API_KEY,
            ddsource: "nodejs",
            service: process.env.WEBSITE_SITE_NAME,
            ddtags: `env:${process.env.NODE_ENV}`
          });

        this._logger = pinoMultiStream({ streams: [
            { stream: process.stdout },
            { stream: streamToDataDog }
        ]});      
    }

    static pushProps(bindings: pino.Bindings){
        return this._logger.child(bindings);
    }
}