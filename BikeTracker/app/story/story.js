(function () {
    'use strict';

    var controllerId = 'storyController';

    angular.module('app').controller(controllerId,
        ['$scope', 'common', story]);

    function story($scope, common) {

        var getLogFn = common.logger.getLogFn;
        var log = getLogFn(controllerId);

        var vm = this;

        $scope.title = 'Story';
        $scope.activate = activate;

        activate();

        function activate() {
            var promises = [];
            common.activateController(promises, controllerId)
                .then(function () { log('Activated Story View'); });
        }
    }
})();
