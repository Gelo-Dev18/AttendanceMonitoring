//function loadCategory() {
//    const selectedCategory = $('#selectedCategory').val(); //Use this kase nasa labas ng event handler instead of  const selectedCategory = $(this).val();
//    const sectionId = $('#sectionId').val();

//    $.ajax({
//        url: '/Admin/AssignSubject',
//        type: 'GET',
//        data: {
//            sectionId: sectionId,
//            category: selectedCategory,
//        },
//        success: function (data) {
//            var newSubjects = $(data).find('#filteredSubject').html();
//            $('#filteredSubject').html(newSubjects);
//        },
//        error: function (xhr, status, error) {
//            console.error('Error selection of category:', error);
//            //    alert('Something went wrong. Please try again.');
//            loadSpinner();
//            loadPageBlur();
//            showDangerToast(response);
//        }
//    })
//}
$(document).ready(function () {

    $(document).on('change', '.subjectCheckbox', function () {
        const anyChecked = $('.subjectCheckbox:checked').length > 0;

        $('#SaveAssignedSubject').prop('disabled', !anyChecked);
    });
 

    $(document).on('input', 'input[name="SearchString"]', function () {
        //Convert all letters to lowercase
        //Remove extra spaces to avoid confused query
        var searchValue = $(this).val().toLowerCase().trim();
        var visibleCount = 0;

        if (searchValue === '') {
            //show all subject if searchbox is empty
            $('input[name="SelectedSubjects"').parent().show();
            $('#noSubjectMessage').hide();
        } else {
            //Filters subjects
            $('input[name="SelectedSubjects"').each(function () {
                //kumuha ng parent element ng check box yung div na nakabalot sa input checkbox
                //kunin yung text content
                var subjectText = $(this).parent().text().toLowerCase().trim();

                if (subjectText.includes(searchValue)) {
                    $(this).parent().show();
                    visibleCount++; //starts to count if the matching of subjects exists
                } else {
                    $(this).parent().hide();
                }
            });

            if (visibleCount === 0) {
                $('#noSubjectMessage').show(); //show if there is no subject matches the input
            } else {
                $('#noSubjectMessage').hide(); // if subject exists, auto hide
            }

         
        }

        //ibig sabihin nito visibleCount++; // Count visible is kapag may subject mag ka count sya
        //then gagana to : else { $('#noSubjectMessage').hide(); }

        //tapos kapag equals sya sa zero eto na gagana nt lalba nayung noSubjectmessage: if (visibleCount === 0) { $('#noSubjectMessage').show(); }
    });

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Select All subject that is visible based on search
    $(document).on('click', "#SelectAllSubjects", function () {
        $('input[name="SelectedSubjects"]:visible').prop('checked', true);
        $('#SaveAssignedSubject').prop('disabled', false);
    });
    //UnSelect All subject that is visible based on search
    $(document).on('click', '#DeselectSubjects', function () {
        $('input[name="SelectedSubjects"]:visible').prop('checked', false);
        $('#SaveAssignedSubject').prop('disabled', true);
    })
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    $(document).on('submit', '#AssignedSubjectForm', function (e) {
        e.preventDefault(); // stops the default page refresh/navigation on form submission, allowing JavaScript to handle the data submission.

        const formData = new FormData(this); //collect and manage form data for submission

        // DEBUG: Print all form data
        console.log('=== FORM DATA ===');
        for (var pair of formData.entries()) {
            console.log(pair[0] + ': ' + pair[1]);
        }
        console.log('=================');
        //RESTful api, two computer system to exchange information through the internet
        $.ajax({
            url: '/Admin/AssignSubject',
            type: 'POST', //a RESTful API uses standard HTTP methods(GET, POST, PUT, DELETE) 
            data: formData, //sends all form data
            processData: false, // para hindi maconvert ni formdata to strings yung submission ng data lalo na if my file included
            contentType: false, //si browser mag set ng contenttype
            success: function (response) {
                //$('#AddTeacherModal').html(response); //after success it will return to Teacher List just like what's on the code in controller .  return PartialView("TeacherList");
                $('.form-control').removeClass('border-danger');
                $('.validation-error-message').text('');
                //$('#validationSum').empty();

                if (response.success) {
                    $('#AssignForm').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#AssignForm').modal('hide');
                    // alert(response.message);
                    loadSpinner();
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    $('#AssignedSubjectModal').html(response);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error assigning subject :', error);
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating assigned subject.');
            }
        });
    });

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    $('.remove-btn').on('click', function (e) {
        e.preventDefault();
        var button = $(this);
        //var button = $(event.relatedTarget); // ginagamit lang kapag modal

        var assignedSubjectId = button.data('assign-id')

        $.ajax({
            url: '/Admin/RemoveAssignedSubject/' + assignedSubjectId,
            type: 'DELETE',
            success: function (response) {
                if (response.success) {
                    showSuccessToast(response.message);
                    //loadSpinner();
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                } else {
                    alert('Could not remove assign');
                }
                //alert(response.message);
                //location.reload();
            },
            error: function (xhr, status, error) {
                console.error('Error deleting Grade:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });

    $(document).on('change', '#selectedCategory', function(){
        //const selectedCategory = $('#selectedCategory').val(); // usethis is nasa labas ng event handler like may function
        const selectedCategory = $(this).val(); //Use this kase nasa loob naman ng event handler, mas faster
        const sectionId = $('#sectionId').val();

        $.ajax({
            url: '/Admin/AssignSubject',
            type: 'GET',
            data: {
                sectionId: sectionId,
                category: selectedCategory,
            },
            success: function (data) {
                var newSubjects = $(data).find('#filteredSubject').html();
                //Replace subject list only not whole page. that's why in razor page, value does not need Viewdata
                $('#filteredSubject').html(newSubjects);

                //Clears the search box if category changed
                $('input[name="SearchString"').val('');
                $('#noSubjectMessage').hide();
                $('#SaveAssignedSubject').prop('disabled', true); //disable button if category is changed
            },
            error: function (xhr, status, error) {
                console.error('Error selection of category:', error);
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        })
    });

    
    //$(document).on('change', '#selectedCategory', function () {
    //    loadCategory();
    //});
});