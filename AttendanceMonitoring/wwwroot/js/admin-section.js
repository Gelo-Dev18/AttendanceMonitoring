function checkGrade() {
    const selectElement = document.getElementById('gradeId');
    const inputElement = document.getElementById('trackId');

    if (selectElement.value === '6' || selectElement.value === '7') {
        inputElement.disabled = false;
    } else {
        inputElement.disabled = true;
        inputElement.value = '';
    }
}

function checkOption() {
    const selectElement = document.getElementById('trackId');
    const inputElement = document.getElementById('TVLInput');

    if (selectElement.value === 'TVL') {
        inputElement.disabled = false;
    } else {
        inputElement.disabled = true;
        inputElement.value = ''; //Optional clear the input field
    }
}
$(document).on('change', '#gradeId', checkGrade); //handle onchange using jqeury so no need onchange inside html tags
$(document).on('change', '#trackId', checkGrade);

$(document).ready(function () {
    //1.Dito, naka-flag na false yung has-assignments so every time na mag open ang modal is naka false sya.
    //Mag automatic syang mag set na true kapag may assigned na bago kaya naka true na sya dun sa '.assign-btn',
    $('#ViewModal').on('show.bs.modal', function () {
        $(this).data('has-assignments', false);
    });

    //This will reload the page for MyClasses list if there is a new assign
    $('#ViewModal').on('hide.bs.modal', function () {
        if ($(this).data('has-assignments')) {
            location.reload();
        }
    });

    $(document).on('submit', '#AddSectionForm', function (e) {
        e.preventDefault(); // stops the default page refresh/navigation on form submission, allowing JavaScript to handle the data submission.

        var formData = new FormData(this); //collect and manage form data for submission
        //RESTful api, two computer system to exchange information through the internet
        $.ajax({
            url: '/Admin/AddSection',
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
                    $('#AddSmallModalForm').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#AddSmallModalForm').modal('hide');
                    // alert(response.message);
                    loadSpinner();
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //$('#AddTeacherModal').html(response);

                    //used to loop over collections of elements, such as arrays, objects, or jQuery objects(collections of DOM elements).
                    //Ang pinaka purpsoe ng code na ito is para gawing border-danger yung textbox na mayroong validation
                    //yung KEY is yung variable or fieldname ko like FirstName, PAssowrd, email etc,
                    //Yung value ayun yung validation error
                    $.each(response.errors, function (key, value) {
                        //dito kapag may error is if yung value.length is > 1 is mag rarun yung code kase may error pero pag wala mag skip ito
                        // yung .length is kung ilang yung element sa loob ng array (yung error) example var errors1 = ["Email is required", "Invalid format"];
                        if (value && value.length > 0) {
                            //Dito hinahanap nyo kung anong input field base sa name attribute. Yung name attribute is ayung "asp-for" so ginagamitan sya ng key kase ayun yung fieldname
                            //then pagnahanap na is lalagyan ng border-danger yung input text box nayun kapag may error
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');
                            //Hinahanap kung may existing error message na sa baba ng input
                            var errorMessageElement = inputElement.next('.validation-error-message');
                            //so dito kapag may error or validation na, is parang irereplace nalang nya ulit yung error kahit same para tapos  ididsplay para hindi mag dagdag or patong yung html element
                            if (errorMessageElement.length > 0) {
                                //so ayun kapag may error is ididsplay and replace lng para di mag patong or dumami error sa html element
                                errorMessageElement.text(value.join(', '));
                            } else {
                                //so dito kapag nag sumbit magrarun yung else code at lalagpas yung if conditon, if wala pang existing na error then maglalagay na ng error
                                //ngayon kapag nag submit tapos same error ulit, dun na tatakbo si IF condition
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }

                    });

                    //$.each(response.errors, function (key, message) {
                    //    $('span[data-valmsg-for="' + key + '"]').text(message);
                    //});
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving Grade :', error);
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating Grade.');
            }
        });
    });

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Submit Function for Edit Teacher
    $(document).on('submit', '#EditSectionForm', function (e) {
        e.preventDefault();

        var formData = new FormData(this);
        //var teacherID = userID;

        //loadPageBlur();

        $('#EditSectionForm .form-control').removeClass('border-danger');
        $('#EditSectionForm .validation-error-message').remove();

        $.ajax({
            url: '/Admin/EditSection/' + userID,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            success: function (response) {
                //console.log('Full response:', response); // Add this line

                if (response.success) {
                    $('#EditSmallModal').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#EditSmallModal').modal('hide');
                    loadSpinner();
                    showUpdateSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {

                    $.each(response.errors, function (key, value) {
                        if (value && value.length > 0) {
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');

                            var errorMessageElement = inputElement.next('.validation-error-message');
                            if (errorMessageElement.length > 0) {
                                errorMessageElement.text(value.join(', '));
                            } else {
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }
                    });
                    //console.log('Errors:', response.errors); // Add this line
                    //console.log('Message:', response.message); // Add this line
                    //    showDangerToast(response);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating Section:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the section.');
            }
        });
    });

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    $('#confirmDeleteButton').on('click', function (e) {
        e.preventDefault();
        console.log('Confirm clicked, userID is:', userID); // Debug log

        if (!userID) {
            alert('Id does not found');
            return;
        }

        //loadSpinner();
        //loadPageBlur(); //Para kapag nag success ang isang submit like adding, deleting or editing

        $.ajax({
            url: '/Admin/DeleteSection/' + userID,
            type: 'DELETE',
            success: function (response) {
                if (response.success) {

                    $('#ModalDelete').off('hide.bs.modal');// for quick fix only //Para i-disable ang blur effect for hiding modal
                    $('#ModalDelete').modal('hide');
                    showSuccessToast(response.message);
                    loadSpinner();
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //    alert('Could not delete Section');
                    showDangerToast(response.message);
                }
                //alert(response.message);
                //location.reload();
            },
            error: function (xhr, status, error) {
                console.error('Error deleting Section:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });

    //$(document).on('submit', '#searchForm', function (e) {
    //    e.preventDefault();

    //    var form = $(this);
    //    var url = form.attr('action');
    //    var data = form.serialize();
    //    $.ajax({
    //        type: 'GET',
    //        url: url,
    //        data: data,
    //        success: function (response) {
    //            $('#mainModalContent').html(response);
    //        },
    //        error: function (xhr, status, error) {
    //            alert('Something went wrong with the search.');
    //            console.error(error);
    //        }
    //    });

    //});

    

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

    $(document).on('click', "#SelectAllSubjects", function () {
        $('input[name="SelectedSubjects"]').prop('checked', true);
    });

    $(document).on('click', '.restore-btn', function (e) {
        e.preventDefault();

        var sectionId = $(this).data("section-id");

        $.ajax({
            url: '/Admin/RestoreSection',
            type: 'POST',
            data: { sectionId: sectionId },
            //processData: false,
            //contentType: false,
            //dataType: 'json',
            success: function (response) {

                $('#ViewModal .modal-body').html(response);
                $('#dataTable2').DataTable();

                showUpdateSuccessToast("Restore Successfully!");
                $('#ViewModal').data('has-assignments', true);
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while restoring section.');
            }
        });
    });
});