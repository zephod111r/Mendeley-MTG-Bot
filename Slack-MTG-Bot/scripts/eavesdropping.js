// Description:
//   This script is responsible for listening for a generic messages and sending responses.
//

const QUEUE_BATCH_SIZE = 32;
const QUEUE_PROCESS_TIMEOUT = 5 * 60; // 5 minutes
const { QueueService, QueueWatcher } = require('../src/QueueWatcher');

module.exports = robot => {
    new QueueWatcher(robot, 'outbound', QUEUE_BATCH_SIZE, QUEUE_PROCESS_TIMEOUT);

    robot.hear(/.+/g, event => {
        const message = event.message.text;
        const from = event.message.user;

        QueueService.createMessage('inbound', JSON.stringify({ from, message }), () => {});
    });
};
