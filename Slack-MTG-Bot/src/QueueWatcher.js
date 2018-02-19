const azure = require('azure');
const storage = require('azure-storage');
const QueueService = azure.createQueueService();
const QueueMessageEncoder = new storage.QueueMessageEncoder.TextBase64QueueMessageEncoder();

QueueService.messageEncoder = QueueMessageEncoder;

class QueueWatcher {
    constructor(robot, queue, size, timeout) {
        this.robot = robot;
        this.queue = queue;
        this.numOfMessages = size;
        this.visibilityTimeout = timeout;

        this.next();
    }

    next() {
        return process.nextTick(this.run.bind(this));
    }

    run() {
        QueueService.getMessages(
            this.queue,
            { numOfMessages: this.numOfMessages, visibilityTimeout: this.visibilityTimeout },
            (error, messages) => {
                if (error) return this.handleError(error);

                for (const { messageText, messageId, popReceipt } of messages) {
                    this.process(messageText);

                    QueueService.deleteMessage(this.queue, messageId, popReceipt, () => {});
                }

                this.next();
            },
        );
    }

    handleError(error) {
        this.robot.logger.error(error);
        this.next();
    }

    process(response) {
        console.log(response);
        this.robot.logger.debug(response);
    }
}

module.exports = {
    QueueWatcher,
    QueueService,
};

/*
{
    color: '#1a2b3c',
    image_url: 'https://mtgbot.blob.core.windows.net/conspiracy3a-take-the-crown/dismiss.jpeg',
    fields: [
        {
            title: '',
            value: '*<http://gatherer.wizards.com/Pages/Card/Details.aspx?name=Dismiss|Dismiss>*',
            short: true,
        },
        { title: '', value: '\u2063:mtg_2: :mtg_u: :mtg_u: ', short: true },
        { title: '', value: 'Instant', short: true },
        { title: '', value: 'Uncommon', short: true },
        { title: '', value: 'Counter target spell.\nDraw a card.' },
    ],
},
*/
