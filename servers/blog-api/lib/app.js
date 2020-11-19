const express = require('express');

const routerFactory = require('./router');
const middlewaresFactory = require('./middlewares');

module.exports = (services) => {
	const {
		conf,
		logger,
	} = services;

	const app = express();
	const middlewares = middlewaresFactory(conf);

	app.use(express.json());

	app.use(routerFactory({
		middlewares,
		services,
	}));

	if (process.env['NODE_ENV']) {
		app.use((err, req, res, next) => { // eslint-disable-line no-unused-vars
			services.logger.error({
				err,
			}, 'Error handler called.');
			return res.sendStatus(500);
		});
	} else {
		const errorhandler = require('errorhandler');

		app.use(errorhandler());
	}

	function listen() {
		const host = conf['HOST'];
		const port = conf['PORT'];

		return app.listen(port, host, () => {
			logger.info({
				host,
				port,
			}, 'Listening.');
		});
	}

	return {
		app,
		listen,
	};
};
