const {
	Router,
} = require('express');
const {
	readdirSync,
} = require('fs');

module.exports = (options) => {
	const router = new Router();

	readdirSync(__dirname + '/routes').forEach((filename) => {
		require('./routes/' + filename)(router, options);
	});

	return router;
};
