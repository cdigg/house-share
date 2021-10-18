houseShare.controller('homeController', ['$scope', 'HomeService', '$modal', function ($scope, homeService, $modal) {
    $scope.tab = 1;
    $scope.loadingBalances = false;
    $scope.loadingMonths = false;
    $scope.sortBal = "to";
    $scope.config = {
        showAllPeople:false
    }

    $scope.datePickers = {
        paidDateOpened: false,
        splitFromOpened: false,
        splitToOpened:false
    }

    $scope.refreshTx = function() {
        $scope.tx = {
            type: null,
            paidDate: moment().format("MM/DD/YYYY"),
            category: "food",
            desc: "",
            amount: "",
            pType: 'single',
            splitFrom: null,
            splitTo: null,
            entityFrom: null,
            entityTo: null,
        }
        $scope.loadShares();
    }

    $scope.greaterThan = function (prop, val) {
        return function (item) {
            return item[prop] > val;
        }
    }

    $scope.inDateRange = function (dte) {
        var mmt = moment(dte);
        return function (item) {
            var current = false;
            $.each(item.dates, function(i, v) {
                if (moment(v.fromStr) <= mmt) {
                    if (v.toStr == "" || moment(v.toStr) >= mmt)
                        current = true;
                }
            });
            return current || $scope.config.showAllPeople;
        }
    }

    $scope.setTab = function(tab) {
        $scope.tab = tab;
        $scope.refreshTx();
    }

    $scope.dateOptions = {
        formatYear: 'yy',
        startingDay: 1,
        showButtonBar: false,
        showWeeks: false
    };
    $scope.openDatePicker = function ($event, which) {
        $event.preventDefault();
        $event.stopPropagation();
        if (which == "p")
            $scope.datePickers.paidDateOpened = true;
        else if (which == "sf")
            $scope.datePickers.splitFromOpened = true;
        else if (which == "st")
            $scope.datePickers.splitToOpened = true;
    };

    $scope.loadShareEntities = function () {
        homeService.getShareEntities().then(function(data) {
            $scope.entities = data;
        });
    }

    $scope.loadShares = function() {
        homeService.loadShares(moment($scope.tx.paidDate).format("MM/DD/YYYY")).then(function (data) {
            $scope.shares = data;
        });
    }

    $scope.addEditEntity = function (entity) {
            var entityInstance = $modal.open({
                templateUrl: 'entity.html',
                controller: 'modalEntity',
                size: "app-modal-window-md",
                resolve: {
                    entity: function () {
                        return entity == null ? null : angular.copy(entity);
                    }
                }
            });

            entityInstance.result.then(function (model) {
                homeService.saveShareEntity(model).then(function() {
                    $scope.loadShareEntities();
                });
            });
    }



    $scope.saveTx = function(type) {
        $scope.tx.type = type;
        $scope.tx.shares = $scope.shares;
        homeService.saveTx($scope.tx).then(function (data) {
            $scope.refreshTx();
            $scope.loadBalances();

            //reload correct month if needed
            var idx = -1;
            angular.forEach($scope.months,
                function (v, i) {
                    if (v.number == data.number && v.year == data.year)
                        idx = i;
                });

            //if month is found and it is already loaded, reload it
            if (idx != -1) {
                if ($scope.months[idx].loaded)
                    $scope.loadMonth($scope.months[idx], true);
            } else {
                //load monthlist if the new month is not found at all, to add the month correctly
                $scope.loadMonths();
            }
        });
    }

    $scope.loadBalances = function () {
        $scope.loadingBalances = true;
        homeService.loadBalances().then(function (data) {
            $scope.balances = data;
            $scope.loadingBalances = false;
        });
    }

    $scope.sortBalance = function (t) {
        if (t == "f") {
            if ($scope.sortBal == "from")
                $scope.sortBal = "-from";
            else
                $scope.sortBal = "from";
        } else {
            if ($scope.sortBal == "to")
                $scope.sortBal = "-to";
            else
                $scope.sortBal = "to";
        }
    }

    $scope.loadMonths = function() {
        $scope.loadingMonths = true;
        homeService.loadMonths().then(function (data) {
            $scope.months = data;
            $scope.loadingMonths = false;
            if($scope.months.length>0)
                $scope.loadMonth($scope.months[0]);
        });
    }

    $scope.recalc = function() {
        homeService.recalcAll().then(function() {
            $scope.loadBalances();
            $scope.loadMonths();
        });
    }

    $scope.loadMonth = function(month,reload) {
        if (!month.loaded || reload) {
            homeService.loadMonth(month.number, month.year).then(function (data) {
                var idx = -1;
                angular.forEach($scope.months,
                    function (v, i) {
                        if (v.number == data.number && v.year == data.year)
                            idx = i;
                    });

                if (idx != -1) {
                    $scope.months[idx].transactions = data.transactions;
                    $scope.months[idx].food = data.food;
                    $scope.months[idx].utilities = data.utilities;
                    $scope.months[idx].misc = data.misc;
                    $scope.months[idx].loaded = true;

                }
            });
        }
    }

    //init
    $scope.loadShareEntities();
    $scope.refreshTx();
    $scope.loadBalances();
    $scope.loadMonths();
}]);


houseShare.controller('modalEntity', function ($scope, $modalInstance, entity) {
    if (entity == null)
        entity = {
            id: null,
            name: "",
            dates:[
                {
                    fromStr: "",
                    toStr: "",
                    shares: 1
                }
            ]
        };
    $scope.entity = entity;

    $scope.save = function () {
        $modalInstance.close($scope.entity);
    }

    $scope.cancel = function () {
        $modalInstance.dismiss('cancel');
    }

    $scope.addDatePeriod = function () {
        $scope.entity.dates.push({
            fromStr: "",
            toStr: "",
            shares: 1
        });
    }
});