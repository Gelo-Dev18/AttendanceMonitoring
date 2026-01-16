document.addEventListener("DOMContentLoaded", function () {

    document.getElementById('academicYear').addEventListener('focus', function () {
        const options = this.options;
        for (let i = 0; i < options.length; i++) {
            if (options[i].text.includes('Active')) {
                options[i].style.color = '#28a745';
                options[i].style.fontWeight = 'bold';
            }
        }
    });


    //1.GET ALL DATES BETWEEN START AND END TO GENERATE LISTS BASE ON FILTERED DATE
    function getAllDatesBetween(startDate, endDate) {
        var dateList = []; //Empty Array to store dates

        //Convert string dates to JS Dates object
        var currentDate = new Date(startDate);
        var lastDate = new Date(endDate);

        //Loop each day from start to end
        while (currentDate <= lastDate) {
            //Add current date(make a copy)
            dateList.push(new Date(currentDate));

            //move to next day
            currentDate.setDate(currentDate.getDate() + 1);
        }
        return dateList;//Return arrays of dates
    }

    //2.FORMAT DATE FOR DISPLAY - Convert date object to readable format like "Mon Dec 2"
    function formatDateForDisplay(date) {
        //Arrays for day and month names
        var dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        var monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
            'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

        //Extract parts from date
        var dayOfWeek = dayNames[date.getDay()]; //e.g, "Mon"
        var monthName = monthNames[date.getMonth()]; //e.g, "Dec"
        var dayOfMonth = date.getDate(); //e.g, 2

        return {
            day: dayOfWeek,
            month: monthName,
            date: dayOfMonth
        };

    }

    //UPDATE DATE RANGE INFO TEST (Show selected date range in readable format)
    function updateDateRangeText() {
        //Get values from date inputs
        var start = new Date($('#startDate').val());
        var end = new Date($('#endDate').val());

        //Calculate how many days
        var allDates = getAllDatesBetween(start, end);
        var numberOfDays = allDates.length;

        //Format dates for display
        var startFormatted = formatDateForDisplay(start);
        var endFormatted = formatDateForDisplay(end);

        //Build text: "Dec 2 - Dec 6, 2025 (5 days)"
        var infoText = startFormatted.month + ' ' + startFormatted.date +
            ' - ' + endFormatted.month + ' ' + endFormatted.date +
            ', ' + start.getFullYear() + //// Call the getFullYear() method on the object to get the year
            ' (' + numberOfDays + ' day';

        //Add "s" sa dulo ng "Day"" text kapag more than 1 day
        if (numberOfDays > 1) {
            infoText += 's';
        }
        infoText += ')';

        $('#dateRangeInfo').text(infoText);

    }

    $('#startDate, #endDate').on('change', function () {
        updateDateRangeText();
    });
    $("#startDate").flatpickr({
        maxDate: "today",
    });
    $("#endDate").flatpickr({
        maxDate: "today",
    });
    updateDateRangeText();

    $('#allTeacher').change(function () {
        var teacherId = $(this).val();
        var classDropdown = $('#gradeFilter');

        classDropdown.empty();
        classDropdown.append('<option value="">Select</option>');

        if (teacherId) {
            $.ajax({
                url: '/Admin/GetTeacherAssignments',
                type: 'GET',
                data: { teacherId: teacherId },
                success: function (classes) {
                    $.each(classes, function (index, cls) {
                        classDropdown.append(
                            $('<option></option>').val(cls.Value).text(cls.Text)
                        );
                    });
                },
                error: function () {
                    alert('Error loading teacher Assignments');
                }
            });
        }
    });
});